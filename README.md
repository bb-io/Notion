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

### Users

- **List users** returns a list of users belonging to the workspace.
- **Get users** returns details about specified user.

## Events

### Pages

- **On pages created** triggers when new pages are created. To allow Blackbird access to the newly created pages you should do so manually through the page options in Notion
- **On pages updated** triggers when any pages are updated.

## HTML Conversion

Blackbird is able to convert `Page` content to an HTML file and back, so that it is possible to translate Notion content automatically via Blackbird.  For example, you can do a combination like "Notion: Get page as HTML" -> "DeepL: Translate" -> "Notion: Update page from HTML". Untranslatable content will be transferred back as well, except of several types like: Reference, File, Child page and Audio. These types won't be transferred for now (after updating the page, these elements will be moved to the top of the page, but they will not be deleted), but we are constantly working to make it better.
  
## Feedback  
  
Do you want to use this app or do you have feedback on our implementation? Reach out to us using the [established channels](https://www.blackbird.io/) or create an issue.
  
<!-- end docs -->