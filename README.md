# Blackbird.io Notion  
  
Blackbird is the new automation backbone for the language technology industry. Blackbird provides enterprise-scale automation and orchestration with a simple no-code/low-code platform. Blackbird enables ambitious organizations to identify, vet and automate as many processes as possible. Not just localization workflows, but any business and IT process. This repository represents an application that is deployable on Blackbird and usable inside the workflow editor.  
  
## Introduction  
  
<!-- begin docs -->  
  
Notion is a note-taking and idea-organizing platform. This Notion application primarily centers around page and database management.  
  
 ## Before setting up
 Before you can connect you need to make sure that:
 - You have a Notion account
 - You have sufficient permissions in the workspace you want to connect Blackbird with.
 
## Connecting  
  
1. Navigate to apps and search for Notion. 
2. Click _Add Connection_.  
3. Name your connection for future reference e.g. 'My Notion connection'.  
4. Click _Authorize connection_.
5. In the popup, select the workspace you want to connect to in the top right corner.
6. Click _Select pages_
7. Manually select all the pages you want Blackbird to access.
8. Click _Allow access_
9. Confirm that the connection has appeared and the status is _Connected_.  
  
> If you later want to give Blackbird the ability to access other pages, you can do so manually through the page options in Notion.

## Actions  
  
### Blocks  
  
- **List block's children** returns all children of a specified block. Instead of block you can input ID of a `Page`, so that it will return all blocks belonging the that page.  
- **Get/Delete block** 

### Comments

- **List comments** returns a list of comments added to the specified `Block`.
- **Add comment** adds comment to a specified `Page` or `Discussion`.

### Databases

- **List databases** returns a list of all databases in the workspace. You can filter the list by create/edited time.
- **List database records** returns a list of all child `Pages` in the database. You can filter the list by create/edited time. 
- **Create/Get database**

### Pages

- **List pages** returns a list of all pages in the workspace. You can filter the list by create/edited time.
- **Get page as HTML** returns specified page's content as an HTML file.
- **Create/Update page from HTML** create/updates page from an HTML file.
- **Get page string/number/date/boolean/files/multiple property** returns the value of a database entry's property of specific type.
- **Set page string/number/boolean/files/multiple property** updates the value of a database entry's property of specific type.
- **Set page property as empty**
- **Create/Get/Archive page**

> For the 'Create page' and 'Create page from HTML' actions, you must specify the parent page or database where the new page will be created. If not specified, the action will fail with an error.

### Users

- **List users** returns a list of users belonging to the workspace.
- **Get users** returns details about specified user.

## Events

### Pages

- **On pages created** triggers when new pages are created. To allow Blackbird access to the newly created pages you should do so manually through the page options in Notion
- **On pages updated** triggers when any pages are updated.

## HTML Conversion

Blackbird can convert `Page` content to an HTML file and back, enabling automatic translation of Notion content via Blackbird. For example, you can set up a workflow like this: "Notion: Get page as HTML" → "DeepL: Translate" → "Notion: Update page from HTML."

Untranslatable content will also be transferred, with a few exceptions. These include:

- **Link preview**
- **Notion-hosted files** (such as PDFs, audio, video, or images; however, external URLs will work fine)

For now, these types won't be transferred. We are continuously working to improve this process.

> Translating child pages and child databases is fully supported. You can translate child pages and/or child databases by setting the 'Include child pages' and 'Include child databases' optional inputs to true for the 'Get page as HTML' action. If these inputs are not set or are set to false, we will not extract or translate child pages/databases, and the new (or updated) page will not include these child pages/databases.

## Feedback  
  
Do you want to use this app or do you have feedback on our implementation? Reach out to us using the [established channels](https://www.blackbird.io/) or create an issue.
  
<!-- end docs -->