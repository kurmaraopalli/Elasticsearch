# .NET 8 Local Elasticsearch Demo (Non-Docker Setup)

This repository contains a simple, high-performance ASP.NET Core Web API that integrates with a local instance of Elasticsearch running directly on your machine. It demonstrates indexing data, full-text multi-field search, fuzzy matching (typo tolerance), and exact term filtering.

---

## 🛠 Prerequisites

*   [.NET 8 SDK](https://microsoft.com) or later.
*   [Elasticsearch 8.x Archive](https://elastic.co) (ZIP for Windows, TAR.GZ for macOS/Linux).

---

## 🚀 Step 1: Set Up & Run Elasticsearch Locally

Because we are not using Docker Desktop, we will run Elasticsearch directly as a local background process.

1.  **Download and Extract**: Extract the downloaded Elasticsearch archive file to a directory of your choice (e.g., `C:\elasticsearch` or `~/elasticsearch`).
2.  **Disable Security for Local Testing**: 
    *   Navigate to the extracted folder and open `config/elasticsearch.yml` in a text editor.
    *   Change the security flag to `false` to simplify local API connection settings:
        ```yaml
        xpack.security.enabled: false
        ```
    *   Save and close the file.
3.  **Start Elasticsearch**: Open a terminal window, navigate to your Elasticsearch folder, and launch the executable:
    *   **Windows:** `bin\elasticsearch.bat`
    *   **macOS / Linux:** `./bin/elasticsearch`
4.  **Verify Status**: Open your browser and navigate to `http://localhost:9200`. You should see a JSON block containing the cluster name and version details. Keep this terminal open.

---

## 💻 Step 2: Build the .NET Web API App

Open a new terminal window to create your project and configure the application files.

### 1. Initialize Project & Install Dependencies
```bash
dotnet new webapi -n ElasticLocalDemo
cd ElasticLocalDemo
dotnet add package Elastic.Clients.Elasticsearch
```

### 2. Create the Data Model
Create a new file named `Product.cs` in the root folder and paste the following layout:

```csharp
namespace ElasticLocalDemo;

public class Product
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Price { get; set; }
    public string Category { get; set; } = string.Empty;
}
```

### 3. Implement the API and Endpoints
Replace the entire contents of your `Program.cs` file with the codebase below. This registers the official client into Dependency Injection and sets up three high-utility API routes:

```csharp
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
                .Fields(f => f.Field(p => p.Name).Field(p => p.Description))
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
```

---

## 🧪 Step 3: Run and Test the API

Start your application server by executing:
```bash
dotnet run
```
*Note the local URL port generated in your terminal (e.g., `http://localhost:5000` or `https://localhost:7001`).*

### 1. Add Data (POST)
Send a `POST` request to `http://localhost:5000/api/products` using Postman, curl, or any API client:

```json
{
  "id": "101",
  "name": "Wireless Ergonomic Mouse",
  "description": "High precision optical tracking mouse with long battery life.",
  "price": 49.99,
  "category": "Electronics"
}
```

### 2. Full-Text Search with Fuzzy Matching (GET)
Test the typo-tolerance feature by searching with a misspelled word:
```text
GET http://localhost:5000/api/products/search?query=mouuse
```
The endpoint will automatically match "mouuse" to the product name "Mouse" using Elasticsearch's fuzziness algorithms.

---

## 📦 Step 4: Push to GitHub

Save your work and publish it to a secure repository for version control:

```bash
git init
dotnet new gitignore
git add .
git commit -m "Initial Commit: Working .NET Local Elasticsearch Demo without Docker"
git branch -M main
git remote add origin https://github.com
git push -u origin main
```
