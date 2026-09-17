using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MemoryAtelierBackend.DTOs;
using MemoryAtelierBackend.Services;

namespace MemoryAtelierBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController(ProductService productService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? categoryId) =>
        Ok(await productService.GetAllAsync(categoryId, User.IsInRole("Admin")));

    [HttpGet("deleted")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDeleted() =>
        Ok(await productService.GetDeletedAsync());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var product = await productService.GetByIdAsync(id, User.IsInRole("Admin"));
        return product == null ? NotFound() : Ok(product);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
    {
        var product = await productService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductDto dto)
    {
        var product = await productService.UpdateAsync(id, dto);
        return product == null ? NotFound() : Ok(product);
    }

    [HttpPatch("{id:guid}/stock")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStock(Guid id, [FromBody] UpdateProductStockDto dto)
    {
        if (dto.Stock < 0)
        {
            return BadRequest("Stock cannot be negative.");
        }

        var product = await productService.UpdateStockAsync(id, dto.Stock);
        return product == null ? NotFound() : Ok(product);
    }

    [HttpPatch("{id:guid}/hidden")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetHidden(Guid id, [FromBody] SetHiddenDto dto)
    {
        var product = await productService.SetHiddenAsync(id, dto.Hidden);
        return product == null ? NotFound() : Ok(product);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await productService.DeleteAsync(id);
        return success ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/restore")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Restore(Guid id)
    {
        var success = await productService.RestoreAsync(id);
        return success ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}/purge")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Purge(Guid id)
    {
        var result = await productService.PurgeAsync(id);
        return result switch
        {
            PurgeResult.Purged => NoContent(),
            PurgeResult.HasReferences => Conflict(new { message = "Продуктът участва в съществуващи поръчки и не може да бъде изтрит завинаги." }),
            _ => NotFound()
        };
    }
}
