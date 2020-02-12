using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models;

namespace Crawler.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CrawlerController : ControllerBase
    {
        // GET: api/Crawler
        //[HttpGet]
        //public async Task<List<Page>> GetAsync()
        //{
        //    return await Crawler.Services.Crawler.Crawl("saipacorp.com",0);
        //}

        [HttpPost]
        public async Task<IEnumerable<Page>> PostAsync([FromBody]Configuration config)
        {
            List<Page> VisitedPages = new List<Page>();
            List<string> Robots = new List<string>();

            await ServiceUtilities.Crawler.Crawl(config, VisitedPages, Robots);
            Response.Headers.Add("X-List-Lenth", VisitedPages.Count().ToString());
            return VisitedPages.Take(1000);
        }

        [HttpPost]
        [Route("GetSingleTitleAsync")]
        public async Task<IActionResult> GetSingleTitleAsync([FromBody]Page page)
        {
            page.Title = await ServiceUtilities.Crawler.LoadTitle(page.Url);
            return Ok(page);
        }

        [HttpPost]
        [Route("GetTitleAsync")]
        public async Task<IActionResult> GetTitleAsync([FromBody]List<Page> pages)
        {
            List<Task> tasks = new List<Task>();
            foreach (var page in pages)
            {
                tasks.Add(Task.Run(() => {
                    page.Title = ServiceUtilities.Crawler.LoadTitle(page.Url).Result;
                }));
                
            }

            await Task.WhenAll(tasks);

            return Ok(pages);
        }

        [HttpPost]
        [Route("PostFormAsync")]
        public async Task<IActionResult> PostFormAsync([FromBody]Page page)
        {
            var links = await Task.Run(()=> ServiceUtilities.Crawler.PostForm(page.Url));
            return Ok(links);
        }
    }
}
