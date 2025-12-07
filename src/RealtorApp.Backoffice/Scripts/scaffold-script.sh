#!/bin/bash

# DbContext
DC="RealtorAppDbContext"

PROJECT="/home/stew/repos/realtorApp/server/src/RealtorApp.Backoffice/RealtorApp.Backoffice.csproj"

# Layout file
LAYOUT="_Layout.cshtml"

# List of entities
ENTITIES=(
  "Agent"
  "AgentsListing"
  "Attachment"
  "Client"
  "ClientInvitation"
  "ClientInvitationsProperty"
  "ClientsListing"
  "ContactAttachment"
  "Conversation"
  "File"
  "FileType"
  "FilesTask"
  "Link"
  "Listing"
  "Message"
  "MessageRead"
  "Notification"
  "Property"
  "PropertyInvitation"
  "RefreshToken"
  "Reminder"
  "Task"
  "TaskAttachment"
  "TaskTitle"
  "Team"
  "ThirdPartyContact"
  "User"
)

for ENTITY in "${ENTITIES[@]}"; do
  CONTROLLER="${ENTITY}sController"
  VIEW_FOLDER="Views/${ENTITY}s"

  echo "Generating controller for $ENTITY..."
  dotnet aspnet-codegenerator controller --controllerName $CONTROLLER --model $ENTITY --dataContext $DC --relativeFolderPath Controllers --useDefaultLayout --layout $LAYOUT --project $PROJECT

  for VIEW in Index Create Edit Details Delete; do
    echo "Generating $VIEW view for $ENTITY..."
    dotnet aspnet-codegenerator view --viewName $VIEW --model $ENTITY --relativeFolderPath $VIEW_FOLDER --useDefaultLayout --layout $LAYOUT --project $PROJECT
  done
done
