using Apps.NotionOAuth.Actions;
using Apps.NotionOAuth.Models.Request.Page.Properties.Getter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tests.Notion.Base;

namespace Tests.Notion
{
    [TestClass]
    public class PageTests :TestBase
    {
        [TestMethod]
        public async Task GetPage_ValidPageId_ShouldReturnPage()
        {
            // Arrange
            var pageId = "1caefdee-ad05-81b4-8767-f65b755f0839";
            var action = new PageActions(InvocationContext,FileManager);
            //var input = new PageStringPropertyRequest
            //{
            //    PageId = "1caefdee-ad05-81b4-8767-f65b755f0839",
            //    DatabaseId = "18cefdee-ad05-80ab-a9fd-d1b5894d9d61",
            //    PropertyId = "Y_%5EN",
            //};
            var input = new PageStringPropertyRequest
            {
                PageId = "1b5efdee-ad05-8100-90af-f0471933c5e6",
                DatabaseId = "18cefdee-ad05-80ab-a9fd-d1b5894d9d61",
                PropertyId = "Y_%5EN",
            };
            // Act
            var page = await action.GetStringProperty(input);
            // Assert
            Assert.IsNotNull(page);
            Console.WriteLine($"Page Title: {page.PropertyValue}");
        }
    }
}
