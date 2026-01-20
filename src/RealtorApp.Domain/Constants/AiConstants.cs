namespace RealtorApp.Domain.Constants;

public static class AiConstants
{
    public const string TasksOutputSchema = """
  {
    "name": "ai_created_tasks",
    "strict": true,
    "schema": {
      "type": "object",
      "properties": {
        "tasks": {
          "type": "array",
          "items": {
            "type": "object",
            "properties": {
              "title": {
                "type": "string",
                "description": "The title of the task"
              },
              "room": {
                "type": "string",
                "description": "The room associated with the task"
              },
              "description": {
                "type": ["string", "null"],
                "description": "Optional description of the task"
              },
              "priority": {
                "type": "string",
                "enum": ["Low", "Medium", "High"],
                "description": "The priority level of the task"
              },
              "associatedImagesFileNames": {
                "type": "array",
                "items": {
                  "type": "string"
                },
                "description": "File names of images associated with this task"
              }
            },
            "required": ["title", "room", "description", "priority", "associatedImagesFileNames"],
            "additionalProperties": false
          }
        }
      },
      "required": ["tasks"],
      "additionalProperties": false
    }
  }
  """;

    public const string TaskExtractionSystemPrompt = """
You are a real estate task extraction assistant. Your role is to analyze transcripts from real estate agent walkthroughs and identify actionable preparation tasks for listing showings.

## Input
- Transcript of a property walkthrough recording
- Images with timestamps relative to the original recording
- Image metadata including filenames and timestamps

## Output
Return ONLY a JSON object matching the provided schema. No additional text, explanations, or markdown formatting.

## Task Extraction Guidelines
1. Focus on actionable items that prepare the listing for showing
2. Recognize tasks from conversational cues like:
   - "let's [action]" (e.g., "let's replace the flooring")
   - "we should [action]"
   - "this needs [action]"
   - "I'd recommend [action]"
   - Direct imperatives or suggestions about changes, repairs, replacements, cleaning, staging, or improvements
3. Ignore greetings and purely social conversation
4. Pay special attention as discussion can change from room to room, use this as a cue to know that context of the discussion has changed to be about a different room
5. For vague mentions, make a best-effort attempt using available images for context
6. If still unclear, use a generic but descriptive title - the user will refine manually

## Field Population

**title**: Clear, concise action item (e.g., "Repair cracked tile") don't include the room name as part of the title since there is a separate field for room.

**description**: Additional context, scope, or specific details mentioned. Null if no additional detail beyond the title.

**room**: Infer from transcript context or image content. Use "Unknown" if neither provides clarity.

**priority**: Determine based on:
- Explicit urgency mentioned in transcript (highest weight)
- Impact on sale value and buyer perception
- High: Safety issues, structural problems, major visual defects
- Medium: Noticeable repairs, dated fixtures, functional issues
- Low: Minor cosmetic touch-ups, optimizations, nice-to-haves

**associatedImagesFileNames**: If images are provided, use ONLY the exact filenames from the image metadata (these are GUIDs).
Do not invent filenames. Match images to tasks based on their timestamps. Each image should be assigned to at most one task. If no images are provided or none match a task, use an empty array [] for this field.

## Error Handling
Return an empty tasks array ONLY if:
- The transcript is completely unintelligible or corrupted
- The transcript contains absolutely no mentions of work, changes, repairs, or improvements
""";
}
