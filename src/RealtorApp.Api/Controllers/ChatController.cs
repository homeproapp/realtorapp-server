using System.Collections;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RealtorApp.Contracts.Commands.Chat.Requests;
using RealtorApp.Contracts.Commands.Chat.Responses;
using RealtorApp.Contracts.Queries.Chat.Requests;
using RealtorApp.Contracts.Queries.Chat.Responses;
using RealtorApp.Domain.Constants;
using RealtorApp.Domain.Interfaces;

namespace RealtorApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting(RateLimitConstants.Authenticated)]
    public class ChatController(IChatService chatService) : RealtorApiBaseController
    {
        private readonly IChatService _chatService = chatService;

        [HttpGet("v1/conversations")]
        public async Task<ActionResult<ConversationListQueryResponse>> ConversationsQuery([FromQuery] ConversationListQuery query)
        {
            if (CurrentUserRole == null)
            {
                return Unauthorized("Unknown or invalid role");
            }

            if (CurrentUserRole == RoleConstants.Agent)
            {
                var conversations = await _chatService.GetAgentConversationListAsync(query, RequiredCurrentUserId);
                if (conversations == null || !string.IsNullOrEmpty(conversations.ErrorMessage))
                {
                    return BadRequest(conversations);
                }

                return Ok(conversations);
            }

            if (CurrentUserRole == RoleConstants.Client)
            {
                var conversations = await _chatService.GetClientConversationList(query, RequiredCurrentUserId);
                if (conversations == null || !string.IsNullOrEmpty(conversations.ErrorMessage))
                {
                    return BadRequest(conversations);
                }

                return Ok(conversations);
            }

            return BadRequest("Unknown error.");
        }

        [HttpGet("v1/conversations/{conversationId}/messages")]
        public async Task<ActionResult<MessageHistoryQueryResponse>> MessagesQuery([FromQuery] MessageHistoryQuery query, [FromRoute] long conversationId)
        {
            var messages = await _chatService.GetMessageHistoryAsync(query, RequiredCurrentUserId, conversationId);

            if (!string.IsNullOrEmpty(messages.ErrorMessage))
            {
                return BadRequest(messages);
            }

            return Ok(messages);
        }

        //TODO: implement better read receipts, my current implementation doesnt work.
        [HttpPost("v1/conversations/{conversationId}/messages/read")]
        public async Task<ActionResult<MarkMessagesAsReadCommandResponse>> MarkMessagesAsReadCommand([FromBody] MarkMessagesAsReadCommand command, [FromRoute] long conversationId)
        {
            await Task.CompletedTask;
            return Ok();
        }
    }
}
