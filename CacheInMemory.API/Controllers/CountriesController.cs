using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CacheInMemory.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CacheInMemory.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CountriesController : ControllerBase
    {
        private readonly ILogger<CountriesController> logger;
        private readonly IMemoryCache memoryCache;

        private const string COUNTRIES_KEY = "Countries";
        private const string REST_COUNTRIES_URL = "https://restcountries.eu/rest/v2/all";

        public CountriesController(ILogger<CountriesController> logger, IMemoryCache cache)
        {
            this.logger = logger;
            this.memoryCache = cache;
        }

        [HttpGet]
        public async Task<IActionResult> GetCountries()
        {
            var sw = new Stopwatch();
            sw.Start();

            if (memoryCache.TryGetValue(COUNTRIES_KEY, out object countriesObject))
            {
                logger.LogInformation("Getting countries from MemoryCache");

                sw.Stop();
                logger.LogInformation($"Time spent in miliseconds: {sw.Elapsed.TotalMilliseconds}");

                return Ok(countriesObject);
            }
            else
            {
                
                logger.LogInformation("Getting countries from API");

                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.GetAsync(REST_COUNTRIES_URL);

                    var responseData = await response.Content.ReadAsStringAsync();
               
                    var countries = JsonConvert.DeserializeObject<List<Country>>(responseData).Take(10);

                    var memoryCacheEntryOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(3600),
                        SlidingExpiration = TimeSpan.FromSeconds(1200),
                        Size = 10
                    };

                    memoryCache.Set(COUNTRIES_KEY, countries, memoryCacheEntryOptions);

                    sw.Stop();
                    logger.LogInformation($"Time spent in miliseconds: {sw.Elapsed.TotalMilliseconds}");

                    return Ok(countries);
                }
            }
        }
    }
}
