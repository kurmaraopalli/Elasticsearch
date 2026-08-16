using Elastic.Clients.Elasticsearch;
using ElasticLocalDemo;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Register Elasticsearch Client pointing to the local archive installation
builder.Services.AddSingleton<ElasticsearchClient>(sp =>
{
    var settings = new ElasticsearchClientSettings(new Uri("http://localhost:9200"))
        .DefaultIndex("products"); // Sets 'products' as the default index name
    
    return new ElasticsearchClient(settings);
});

var app = builder.Build();
app.UseHttpsRedirection();

// ---- API ENDPOINTS ----

// 1. Index (Create/Update) a Product
app.MapPost("/api/products", async (Product product, ElasticsearchClient client) =>
{
    var response = await client.IndexAsync(product);
    
    if (!response.IsValidResponse)
        return Results.Problem(response.DebugInformation);

    return Results.Ok(new { Message = "Product indexed successfully", Id = product.Id });
});

// 2. Search Products by keyword in Name or Description (With Typo Tolerance)
app.MapGet("/api/products/search", async ([FromQuery] string query, ElasticsearchClient client) =>
{
    if (string.IsNullOrWhiteSpace(query))
        return Results.BadRequest("Search query cannot be empty");

    var response = await client.SearchAsync<Product>(s => s
        .Query(q => q
            .MultiMatch(mm => mm
                .Fields(new Field("name"), new Field("description"))
                .Query(query)
                .Fuzziness(new Fuzziness("AUTO")) // Automatically handles typos
            )
        )
    );

    if (!response.IsValidResponse)
        return Results.Problem(response.DebugInformation);

    var results = response.Hits.Select(h => h.Source).ToList();
    return Results.Ok(results);
});

// 3. Filter Products by Category (Exact Match)
app.MapGet("/api/products/category/{categoryName}", async (string categoryName, ElasticsearchClient client) =>
{
    var response = await client.SearchAsync<Product>(s => s
        .Query(q => q
            .Term(t => t
                .Field(p => p.Category)
                .Value(categoryName)
            )
        )
    );

    if (!response.IsValidResponse)
        return Results.Problem(response.DebugInformation);

    var results = response.Hits.Select(h => h.Source).ToList();
    return Results.Ok(results);
});

app.Run();
