- update tables to account for subscriptions
- limit agents to 1 active property if on free plan
- add partial indexes for deleted_at column.
    - might come in handy for some tables..
- make uuid required again, doesnt need to be nullable anymore ✅
- invite client to existing property
- consider how sendmessageasync can set isread to true if receiving user is connected to hub
- Set policies on endpoints for agentonly or clientonly
- when creating property after invite, handle creating default conversation for each property
    - also consider removing old convo and creating new one if property exists

- check cache invalidation
    especially for user associations to listings

- add limits to text areas
    - chat
        - send as multiple messages
    - task description
    - reminder text
    - find some way to set a global limit
