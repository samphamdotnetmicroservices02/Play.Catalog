using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Play.Catalog.Contracts;
using Play.Catalog.Service.Dtos;
using Play.Catalog.Service.Entities;
using Play.Common;
using Play.Common.Settings;

namespace Play.Catalog.Service.Controllers
{
    [ApiController]
    [Route("items")]
    [Authorize(Roles = AdminRole)]
    public class ItemsController : ControllerBase
    {
        private const string AdminRole = "Admin";
        private readonly IRepository<Item> _itemsRepository;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly Counter<int> _itemUpdatedCounter;

        //For test CircuitBreaker and exponential backoff purpose
        //private static int requestCounter = 0;

        public ItemsController(IRepository<Item> itemsRepository, IPublishEndpoint publishEndpoint, IConfiguration configuration)
        {
            _itemsRepository = itemsRepository;
            _publishEndpoint = publishEndpoint;

            /*
            * Premetheus: we're going to be needing the service name of our microservice to define what we call a Meter that will also
            * lat us create the counters. The Meter is the entry point for all the metrics tracking of your microservice. So usually 
            * you'll have at least one Meter that owns everything related to metrics in your microservice.
            */
            var settings = configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();
            Meter meter = new(settings.ServiceName);
            _itemUpdatedCounter = meter.CreateCounter<int>("ItemUpdated");
        }

        [HttpGet]
        [Authorize(Policies.Read)]
        public async Task<ActionResult<IEnumerable<ItemDto>>> GetAsync()
        {
            //For test CircuitBreaker and exponential backoff purpose
            // requestCounter++;
            // Console.WriteLine($"Request {requestCounter}: Starting...");

            // if (requestCounter <= 2)
            // {
            //     Console.WriteLine($"Request {requestCounter}: Delaying...");
            //     await Task.Delay(TimeSpan.FromSeconds(10));
            // }

            // if (requestCounter <= 4)
            // {
            //     Console.WriteLine($"Request {requestCounter}: 500 (Internal Server Error).");
            //     return StatusCode(500);
            // }

            var items = (await _itemsRepository.GetAllAsync())
                .Select(item => item.AsDto());

            //Console.WriteLine($"Request {requestCounter}: 200 (OK).");
            return Ok(items);
        }

        // GET /items/{id}
        [HttpGet("{id}")]
        [Authorize(Policies.Read)]
        public async Task<ActionResult<ItemDto>> GetByIdAsync(Guid id)
        {
            var item = await _itemsRepository.GetAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            return item.AsDto();
        }

        // POST /items
        [HttpPost]
        [Authorize(Policies.Write)]
        public async Task<ActionResult<ItemDto>> PostAsync(CreateItemDto createItemDto)
        {
            var item = new Item
            {
                Name = createItemDto.Name,
                Description = createItemDto.Description,
                Price = createItemDto.Price,
                CreatedDate = DateTimeOffset.UtcNow,
            };

            await _itemsRepository.CreateAsync(item);
            await _publishEndpoint.Publish(new CatalogItemCreated(
                item.Id,
                item.Name,
                item.Description,
                item.Price));

            return CreatedAtAction(nameof(GetByIdAsync), new { id = item.Id }, item);
        }

        // PUT /items/{id}
        [HttpPut("{id}")]
        [Authorize(Policies.Write)]
        public async Task<IActionResult> PutAsync(Guid id, UpdateItemDto updateItemDto)
        {
            var existingItem = await _itemsRepository.GetAsync(id);

            if (existingItem is null)
                return NotFound();

            existingItem.Name = updateItemDto.Name;
            existingItem.Description = updateItemDto.Description;
            existingItem.Price = updateItemDto.Price;

            await _itemsRepository.UpdateAsync(existingItem);
            _itemUpdatedCounter.Add(1, new KeyValuePair<string, object>("ItemId", id)); // boxing ItemId to object
            await _publishEndpoint.Publish(new CatalogItemUpdated(
                existingItem.Id,
                existingItem.Name,
                existingItem.Description,
                existingItem.Price));

            return NoContent();
        }

        // DELETE /items/{id}
        [HttpDelete("{id}")]
        [Authorize(Policies.Write)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var item = await _itemsRepository.GetAsync(id);

            if (item is null)
                return NotFound();

            await _itemsRepository.RemoveAsync(item.Id);
            await _publishEndpoint.Publish(new CatalogItemDeleted(item.Id));

            return NoContent();
        }
    }
}