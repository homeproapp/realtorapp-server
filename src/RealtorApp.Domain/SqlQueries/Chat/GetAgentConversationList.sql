WITH client_groups AS (
    -- Group clients by property to create unique client sets per conversation
    SELECT
        STRING_AGG(cp.client_id::text, ',' ORDER BY cp.client_id) as client_group_key,
        cp.conversation_id,
        ARRAY_AGG(cp.client_id ORDER BY cp.client_id) as client_ids
    FROM clients_properties cp
    WHERE cp.agent_id = {0} AND cp.deleted_at IS NULL
    GROUP BY cp.conversation_id
),
client_set_groups AS (
    -- Group conversations that have identical client sets
    SELECT
        client_group_key,
        ARRAY_AGG(conversation_id ORDER BY (
            SELECT c.updated_at FROM conversations c WHERE c.conversation_id = cg.conversation_id
        ) DESC) as conversation_ids,
        client_ids,
        -- Most recent conversation for click-through
        (ARRAY_AGG(conversation_id ORDER BY (
            SELECT c.updated_at FROM conversations c WHERE c.conversation_id = cg.conversation_id
        ) DESC))[1] as click_through_conversation_id
    FROM client_groups cg
    GROUP BY client_group_key, client_ids
),
ranked_results AS (
    SELECT
        csg.click_through_conversation_id,
        {0} as agent_id,
        -- Get most recent message across all conversations in this group
        (
            SELECT m.message_id
            FROM messages m
            WHERE m.conversation_id = ANY(csg.conversation_ids)
              AND m.deleted_at IS NULL
            ORDER BY m.created_at DESC
            LIMIT 1
        ) as message_id,
        (
            SELECT m.message_text
            FROM messages m
            WHERE m.conversation_id = ANY(csg.conversation_ids)
              AND m.deleted_at IS NULL
            ORDER BY m.created_at DESC
            LIMIT 1
        ) as message_text,
        (
            SELECT m.sender_id
            FROM messages m
            WHERE m.conversation_id = ANY(csg.conversation_ids)
              AND m.deleted_at IS NULL
            ORDER BY m.created_at DESC
            LIMIT 1
        ) as message_sender_id,
        (
            SELECT m.created_at
            FROM messages m
            WHERE m.conversation_id = ANY(csg.conversation_ids)
              AND m.deleted_at IS NULL
            ORDER BY m.created_at DESC
            LIMIT 1
        ) as message_created_at,
        -- Count conversations with unread messages
        (
            SELECT COUNT(DISTINCT conv_id)
            FROM unnest(csg.conversation_ids) as conv_id
            WHERE EXISTS (
                SELECT 1 FROM messages m
                WHERE m.conversation_id = conv_id
                  AND m.deleted_at IS NULL
                  AND m.is_read = false
                  AND m.sender_id != {0}
            )
        ) as unread_conversation_count,
        -- Get client names as JSON
        (
            SELECT STRING_AGG(
                u.user_id || ':' || TRIM(COALESCE(u.first_name, '') || ' ' || COALESCE(u.last_name, '')),
                '|'
                ORDER BY u.user_id
            )
            FROM unnest(csg.client_ids) as client_id
            JOIN users u ON u.user_id = client_id
        ) as client_names_data,
        ROW_NUMBER() OVER (
            ORDER BY (
                SELECT m.created_at
                FROM messages m
                WHERE m.conversation_id = ANY(csg.conversation_ids)
                  AND m.deleted_at IS NULL
                ORDER BY m.created_at DESC
                LIMIT 1
            ) DESC NULLS LAST
        ) as row_num
    FROM client_set_groups csg
),
paginated_results AS (
    SELECT *
    FROM ranked_results
    WHERE row_num > {1} AND row_num <= {1} + {2}
),
total_count AS (
    SELECT COUNT(*) as total_count FROM ranked_results
)
SELECT
    pr.click_through_conversation_id as ClickThroughConversationId,
    pr.agent_id as AgentId,
    pr.message_id as MessageId,
    pr.message_text as MessageText,
    pr.message_sender_id as SenderId,
    pr.message_created_at as CreatedAt,
    pr.unread_conversation_count as UnreadConversationCount,
    pr.client_names_data as ClientNamesData,
    tc.total_count as TotalCount
FROM paginated_results pr
CROSS JOIN total_count tc
ORDER BY pr.row_num;