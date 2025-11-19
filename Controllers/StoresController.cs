using Challenge.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Challenge.Controllers;

public class StoresController : Controller
{
    private readonly ApplicationDbContext _context;

    public StoresController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var stores = await _context.Stores
                .Include(s => s.Transactions)
                .OrderBy(s => s.Name)
                .ToListAsync();

            return View(stores);
        }
        catch (Exception)
        {
            // If database doesn't exist yet, return empty list
            return View(new List<Models.Store>());
        }
    }
}

