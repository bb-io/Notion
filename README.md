# Blackbird.io Notion  
  
Blackbird is the new automation backbone for the language technology industry. Blackbird provides enterprise-scale automation and orchestration with a simple no-code/low-code platform. Blackbird enables ambitious organizations to identify, vet and automate as many processes as possible. Not just localization workflows, but any business and IT process. This repository represents an application that is deployable on Blackbird and usable inside the workflow editor.  
  
## Introduction  
  
<!-- begin docs -->  
  
Notion is a note-taking and idea-organizing platform. This Notion application primarily centers around page and database management.  
  
 ## Before setting up
 - Go to `https://www.notion.so/my-integrations` and click _New integration_
 - Fill in all the required fields
 - Open _Secrets_ tab and save  _Internal Integration Secret_ value to be able to connect to Notion via Blackbird.

## Add connection to pages and databases

To make your content interactive with Blackbird, you should add your newly created connection to each page or database you want to work with. Connection will be automatically added to their children, If there are any.

#### To add your connection to the page or database:

- Open your page or database
- Click on the three dots in the top right corner
- Go to the _Connections_ section and hover on _Add connections_
- Select your newly created connection
- Click _Confirm_
 
## Connecting  
  
1. Navigate to apps and search for Notion. If you cannot find Notion then click _Add App_ in the top right corner, select Notion and add the app to your Blackbird environment.  
2. Click _Add Connection_.  
3. Name your connection for future reference e.g. 'My client'.  
6. Paste your _Internal Integration Secret_ it to the appropriate field in the Blackbird  
7. Click _Connect_.  
8. Confirm that the connection has appeared and the status is _Connected_.  
  
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
- **Create/Get/Archive page**

### Users

- **List users** returns a list of users belonging to the workspace.
- **Get users** returns details about specified user.

## HTML Conversion

Blackbird is able to convert `Page` content to an HTML file and back, so that it is possible to translate Notion content automatically via Blackbird.  For example, you can do a combination like "Notion: Get page as HTML" -> "DeepL: Translate" -> "Notion: Update page from HTML". Untranslatable content will be transferred back as well, except of several types like: Reference, Image, File, Link. These types won't be transferred for now, but we are constantly working to make it better.
  
## Feedback  
  
Do you want to use this app or do you have feedback on our implementation? Reach out to us using the [established channels](https://www.blackbird.io/) or create an issue.
  
<!-- end docs -->