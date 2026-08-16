# .NET 8 Local Elasticsearch Demo (Non-Docker Setup)

This repository contains a simple, high-performance ASP.NET Core Web API that integrates with a local instance of Elasticsearch running directly on your machine. It demonstrates indexing data, full-text multi-field search, fuzzy matching (typo tolerance), and exact term filtering.

---

## 🛠 Prerequisites

*   [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
*   [Elasticsearch 8.x](https://www.elastic.co/downloads/elasticsearch) (ZIP for Windows, TAR.GZ for macOS/Linux)
*   A code editor (Visual Studio, VS Code, or any preferred IDE)
*   Postman, curl, or any API testing tool

---

## 🚀 Step 1: Set Up & Run Elasticsearch Locally

Because we are not using Docker Desktop, we will run Elasticsearch directly as a local background process.

1.  **Download and Extract**: 
    *   Download Elasticsearch 8.x from the official website
    *   Extract the archive to a directory of your choice (e.g., `C:\elasticsearch` on Windows or `~/elasticsearch` on macOS/Linux)

2.  **Configure Elasticsearch for Local Development**: 
    *   Navigate to the extracted folder and open `config/elasticsearch.yml` in a text editor
    *   Add or modify the following line to disable security for simpler local testing:
        ```yaml
        xpack.security.enabled: false
        ```
    *   Save and close the file

3.  **Start Elasticsearch**: 
    *   Open a terminal/command prompt
    *   Navigate to your Elasticsearch directory
    *   Run the appropriate command for your OS:
        *   **Windows:** `bin\elasticsearch.bat`
        *   **macOS / Linux:** `./bin/elasticsearch`
    *   Keep this terminal window open - Elasticsearch needs to remain running

4.  **Verify Elasticsearch is Running**: 
    *   Open your browser and navigate to `http://localhost:9200`
    *   You should see a JSON response with cluster name, version, and other details
    *   If you see this response, Elasticsearch is running successfully

---

## 💻 Step 2: Build the .NET Web API App

Open a new terminal window (keep Elasticsearch running in the first terminal) to create your project and configure the application files.

### 1. Initialize Project & Install Dependencies
```bash
# Create a new Web API project
dotnet new webapi -n ElasticLocalDemo

# Navigate into the project directory
cd ElasticLocalDemo

# Install the official Elasticsearch client for .NET
dotnet add package Elastic.Clients.Elasticsearch
```

### 2. Create the Data Model
Create a new file named `Product.cs` in the root of your project and add the following code:

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
Replace the entire contents of your `Program.cs` file with the code below. This registers the Elasticsearch client via Dependency Injection and sets up three API endpoints:

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

### 1. Start the .NET Application
Navigate to your project directory and run the application:
```bash
cd ElasticLocalDemo
dotnet run
```
**Important:** Note the local URL port shown in your terminal output (e.g., `http://localhost:5000` or `https://localhost:7001`). You'll need this URL for testing.

### 2. Test the API Endpoints

#### A. Add Sample Products (POST)
Use Postman, curl, or any API client to add products to Elasticsearch. Send POST requests to `http://localhost:5000/api/products`:

**Sample Product 1:**
```json
{
  "id": "101",
  "name": "Wireless Ergonomic Mouse",
  "description": "High precision optical tracking mouse with long battery life.",
  "price": 49.99,
  "category": "Electronics"
}
```

**Sample Product 2:**
```json
{
  "id": "102",
  "name": "Mechanical Gaming Keyboard",
  "description": "RGB backlit mechanical keyboard with cherry switches.",
  "price": 89.99,
  "category": "Electronics"
}
```

**Sample Product 3:**
```json
{
  "id": "103",
  "name": "USB-C Hub",
  "description": "Multi-port USB-C hub with HDMI and card reader.",
  "price": 34.99,
  "category": "Accessories"
}
```

#### B. Full-Text Search with Fuzzy Matching (GET)
Test the typo-tolerance feature by searching with misspelled words:
```text
GET http://localhost:5000/api/products/search?query=mouuse
```
This will match "mouuse" to "Mouse" using Elasticsearch's fuzzy matching.

**Try these searches:**
- `query=wireless` - Should return the wireless mouse
- `query=keybord` - Should return the keyboard (typo tolerance)
- `query=usb` - Should return the USB-C hub

#### C. Filter by Category (GET)
Filter products by exact category match:
```text
GET http://localhost:5000/api/products/category/Electronics
```

**Try these categories:**
- `Electronics` - Should return mouse and keyboard
- `Accessories` - Should return USB-C hub

---

## 📦 Step 4: Push to GitHub

### 1. Initialize Git Repository
Navigate to your project directory and initialize git:

```bash
cd ElasticLocalDemo
git init
```

### 2. Create .gitignore File
Generate a standard .NET gitignore file to exclude build artifacts and dependencies:

```bash
dotnet new gitignore
```

### 3. Commit Your Changes
Stage all files and create your initial commit:

```bash
git add .
git commit -m "Initial commit: .NET 8 Elasticsearch integration demo"
```

### 4. Create GitHub Repository and Push
**Option A: Using GitHub CLI (if installed)**
```bash
gh repo create elastic-local-demo --public --source=. --remote=origin --push
```

**Option B: Manual GitHub Setup**
1. Go to [GitHub](https://github.com) and create a new repository
2. Copy the repository URL
3. Add the remote and push:
```bash
git branch -M main
git remote add origin https://github.com/YOUR_USERNAME/YOUR_REPO_NAME.git
git push -u origin main
```

---

## 🔧 Troubleshooting

### Elasticsearch Issues
- **Port 9200 already in use:** Check if another Elasticsearch instance is running or change the port in `config/elasticsearch.yml`
- **Connection refused:** Ensure Elasticsearch is running before starting the .NET application
- **Java not found:** Elasticsearch requires Java. Install JDK 11 or later

### .NET Application Issues
- **Package restore errors:** Run `dotnet restore` to ensure all packages are properly restored
- **Port conflicts:** The application may use a different port if 5000 is occupied. Check the terminal output for the actual URL
- **CORS errors:** If testing from a browser, you may need to add CORS configuration to `Program.cs`

### API Testing Issues
- **404 errors:** Verify the URL port matches what's shown in the terminal when running `dotnet run`
- **No search results:** Ensure you've added products via POST before attempting to search
- **Connection timeout:** Check that Elasticsearch is still running in its terminal window

---

## 📚 Additional Resources

- [Elasticsearch .NET Client Documentation](https://www.elastic.co/guide/en/elasticsearch/client/net-api/current/index.html)
- [Elasticsearch Query DSL](https://www.elastic.co/guide/en/elasticsearch/reference/current/query-dsl.html)
- [ASP.NET Core Web API Documentation](https://docs.microsoft.com/aspnet/core/web-api/)

---

## 📝 Project Structure

```
ElasticLocalDemo/
├── Program.cs              # Main application file with API endpoints
├── Product.cs              # Product data model
├── ElasticLocalDemo.csproj # Project file with dependencies
├── appsettings.json        # Application configuration
└── Properties/
    └── launchSettings.json # Development server configuration
```
