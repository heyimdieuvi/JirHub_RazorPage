# ASP.NET Core MVC - View Layer Implementation Guide

> **Purpose**: A comprehensive guide for implementing the View Layer in ASP.NET Core MVC applications based on best practices and proven patterns.

---

## 📋 Table of Contents

1. [View Layer Overview](#view-layer-overview)
2. [Project Structure Setup](#project-structure-setup)
3. [Core Configuration Files](#core-configuration-files)
4. [Layout and Shared Components](#layout-and-shared-components)
5. [View Patterns and Examples](#view-patterns-and-examples)
6. [Data Passing Mechanisms](#data-passing-mechanisms)
7. [Tag Helpers Reference](#tag-helpers-reference)
8. [Form Handling Patterns](#form-handling-patterns)
9. [Client-Side Integration](#client-side-integration)
10. [Best Practices Checklist](#best-practices-checklist)

---

## View Layer Overview

### What is the View Layer?

The **View Layer** is the presentation layer in ASP.NET Core MVC responsible for:
- Rendering HTML to users
- Displaying data from controllers
- Capturing user input through forms
- Providing interactive UI elements

### Technologies Used
- **Razor View Engine** (.cshtml files)
- **Tag Helpers** (cleaner syntax than HTML helpers)
- **Bootstrap 5** (responsive UI framework)
- **jQuery** (client-side functionality)
- **Client-side validation** (unobtrusive validation)

### Key Principles
1. **Separation of Concerns**: Views only handle presentation, not business logic
2. **Strongly-Typed Models**: Use `@model` directive for type safety
3. **DRY (Don't Repeat Yourself)**: Use layouts and partial views
4. **Convention over Configuration**: Follow MVC naming conventions

---

## Project Structure Setup

### Recommended Folder Structure

```
Views/
├── _ViewStart.cshtml              # Sets default layout
├── _ViewImports.cshtml            # Global imports and tag helpers
│
├── Shared/                        # Shared/Reusable components
│   ├── _Layout.cshtml             # Main application layout
│   ├── _Layout.cshtml.css         # Layout-specific styles
│   ├── Error.cshtml               # Error page
│   └── _ValidationScriptsPartial.cshtml
│
├── [ControllerName]/              # Views per controller
│   ├── Index.cshtml               # List/display view
│   ├── Create.cshtml              # Form to create new item
│   ├── Edit.cshtml                # Form to edit existing item
│   ├── Details.cshtml             # Show single item details
│   └── Delete.cshtml              # Delete confirmation
│
└── Account/                       # Authentication views
    ├── Login.cshtml
    ├── Register.cshtml
    └── Forbidden.cshtml
```

### Naming Conventions

| Convention | Example | Description |
|------------|---------|-------------|
| Controller | `ProductController.cs` | Controller name |
| View Folder | `Views/Product/` | Matches controller name |
| View File | `Index.cshtml` | Matches action method name |
| Layout | `_Layout.cshtml` | Prefix with underscore |
| Partial View | `_ProductCard.cshtml` | Prefix with underscore |
| View Model | `ProductViewModel.cs` | Suffix with ViewModel |

---

## Core Configuration Files

### 1. `_ViewStart.cshtml`

**Purpose**: Sets the default layout for all views in the folder and subfolders.

**Location**: `Views/_ViewStart.cshtml`

```csharp
@{
    Layout = "_Layout";
}
```

**Override in specific view**:
```csharp
@{
    Layout = null;  // No layout
    // OR
    Layout = "_AlternativeLayout";  // Different layout
}
```

---

### 2. `_ViewImports.cshtml`

**Purpose**: Provides global imports and tag helper registrations for all views.

**Location**: `Views/_ViewImports.cshtml`

```csharp
@using YourProjectName
@using YourProjectName.Models
@using YourProjectName.ViewModels
@using Microsoft.AspNetCore.Identity
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
```

**What to include**:
- Common namespaces used across views
- Tag helper registrations
- Custom tag helpers (if any)

---

### 3. `_Layout.cshtml` (Master Layout)

**Purpose**: Main template that wraps all content pages.

**Location**: `Views/Shared/_Layout.cshtml`

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - Your Application</title>
    
    <!-- CSS References -->
    <link rel="stylesheet" href="~/lib/bootstrap/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
    
    <!-- Optional: Additional CSS section -->
    @await RenderSectionAsync("Styles", required: false)
</head>
<body>
    <!-- Navigation Bar -->
    <header>
        <nav class="navbar navbar-expand-sm navbar-toggleable-sm navbar-light bg-white border-bottom box-shadow mb-3">
            <div class="container-fluid">
                <a class="navbar-brand" asp-area="" asp-controller="Home" asp-action="Index">
                    Your App Name
                </a>
                
                <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target=".navbar-collapse">
                    <span class="navbar-toggler-icon"></span>
                </button>
                
                <div class="navbar-collapse collapse d-sm-inline-flex justify-content-between">
                    <ul class="navbar-nav flex-grow-1">
                        <li class="nav-item">
                            <a class="nav-link text-dark" asp-controller="Home" asp-action="Index">Home</a>
                        </li>
                        <li class="nav-item">
                            <a class="nav-link text-dark" asp-controller="Product" asp-action="Index">Products</a>
                        </li>
                    </ul>
                    
                    <!-- User Authentication Section -->
                    <partial name="_LoginPartial" />
                </div>
            </div>
        </nav>
    </header>
    
    <!-- Main Content Area -->
    <div class="container">
        <main role="main" class="pb-3">
            <!-- This is where child view content appears -->
            @RenderBody()
        </main>
    </div>
    
    <!-- Footer -->
    <footer class="border-top footer text-muted">
        <div class="container">
            &copy; 2026 - Your Application - <a asp-controller="Home" asp-action="Privacy">Privacy</a>
        </div>
    </footer>
    
    <!-- JavaScript References -->
    <script src="~/lib/jquery/dist/jquery.min.js"></script>
    <script src="~/lib/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
    <script src="~/js/site.js" asp-append-version="true"></script>
    
    <!-- Optional: Scripts section for page-specific JavaScript -->
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

**Key Components**:
- `@ViewData["Title"]` - Dynamic page title
- `@RenderBody()` - Placeholder for child view content
- `@RenderSectionAsync("Scripts")` - Optional scripts section
- `asp-append-version="true"` - Cache busting for static files

---

## Layout and Shared Components

### Partial Views

**Purpose**: Reusable UI components that can be included in multiple views.

**Example: `_LoginPartial.cshtml`**
```razor
@if (User.Identity.IsAuthenticated)
{
    <ul class="navbar-nav">
        <li class="nav-item">
            <span class="navbar-text">Hello, @User.Identity.Name!</span>
        </li>
        <li class="nav-item">
            <form asp-controller="Account" asp-action="Logout" method="post">
                <button type="submit" class="btn btn-link nav-link">Logout</button>
            </form>
        </li>
    </ul>
}
else
{
    <ul class="navbar-nav">
        <li class="nav-item">
            <a class="nav-link" asp-controller="Account" asp-action="Login">Login</a>
        </li>
        <li class="nav-item">
            <a class="nav-link" asp-controller="Account" asp-action="Register">Register</a>
        </li>
    </ul>
}
```

**Usage in views**:
```razor
<partial name="_LoginPartial" />
<!-- OR -->
@await Html.PartialAsync("_LoginPartial")
```

---

### Validation Scripts Partial

**Location**: `Views/Shared/_ValidationScriptsPartial.cshtml`

```html
<script src="~/lib/jquery-validation/dist/jquery.validate.min.js"></script>
<script src="~/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.min.js"></script>
```

**Usage in forms**:
```razor
@section Scripts {
    @{await Html.RenderPartialAsync("_ValidationScriptsPartial");}
}
```

---

## View Patterns and Examples

### Pattern 1: List/Index View

**Purpose**: Display a collection of items in a table or list.

**Model Type**: `IEnumerable<T>`

```razor
@model IEnumerable<YourProject.Models.Product>

@{
    ViewData["Title"] = "Products";
}

<h2>Product List</h2>

<!-- Success/Error Messages -->
@if (TempData["SuccessMessage"] != null)
{
    <div class="alert alert-success alert-dismissible fade show">
        @TempData["SuccessMessage"]
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    </div>
}

<!-- Action Buttons -->
<p>
    <a asp-action="Create" class="btn btn-primary">
        <i class="fa fa-plus"></i> Create New
    </a>
</p>

<!-- Search Form -->
<form asp-action="Index" method="get" class="mb-3">
    <div class="row g-2">
        <div class="col-md-4">
            <input type="text" name="searchTerm" class="form-control" 
                   placeholder="Search by name..." 
                   value="@ViewData["CurrentFilter"]" />
        </div>
        <div class="col-md-2">
            <button type="submit" class="btn btn-secondary">Search</button>
            <a asp-action="Index" class="btn btn-outline-secondary">Clear</a>
        </div>
    </div>
</form>

<!-- Data Table -->
<div class="table-responsive">
    <table class="table table-striped table-hover">
        <thead class="table-dark">
            <tr>
                <th>ID</th>
                <th>Name</th>
                <th>Price</th>
                <th>Category</th>
                <th>Actions</th>
            </tr>
        </thead>
        <tbody>
            @if (Model != null && Model.Any())
            {
                @foreach (var item in Model)
                {
                    <tr>
                        <td>@item.Id</td>
                        <td>@item.Name</td>
                        <td>@item.Price.ToString("C")</td>
                        <td>@item.Category?.Name</td>
                        <td>
                            <a asp-action="Details" asp-route-id="@item.Id" 
                               class="btn btn-sm btn-info">Details</a>
                            <a asp-action="Edit" asp-route-id="@item.Id" 
                               class="btn btn-sm btn-warning">Edit</a>
                            <a asp-action="Delete" asp-route-id="@item.Id" 
                               class="btn btn-sm btn-danger">Delete</a>
                        </td>
                    </tr>
                }
            }
            else
            {
                <tr>
                    <td colspan="5" class="text-center text-muted">
                        No products found.
                    </td>
                </tr>
            }
        </tbody>
    </table>
</div>

<!-- Pagination (if applicable) -->
@if (ViewBag.TotalPages > 1)
{
    <nav>
        <ul class="pagination">
            @for (int i = 1; i <= ViewBag.TotalPages; i++)
            {
                <li class="page-item @(i == ViewBag.CurrentPage ? "active" : "")">
                    <a class="page-link" asp-action="Index" asp-route-page="@i">@i</a>
                </li>
            }
        </ul>
    </nav>
}
```

**Controller Action**:
```csharp
public async Task<IActionResult> Index(string searchTerm, int page = 1)
{
    ViewData["CurrentFilter"] = searchTerm;
    
    var products = await _productService.SearchAsync(searchTerm, page);
    
    ViewBag.CurrentPage = page;
    ViewBag.TotalPages = await _productService.GetTotalPagesAsync(searchTerm);
    
    return View(products);
}
```

---

### Pattern 2: Create/Edit Form View

**Purpose**: Form for creating or editing an entity.

**Model Type**: Single entity `T`

```razor
@model YourProject.Models.Product

@{
    ViewData["Title"] = Model.Id == 0 ? "Create Product" : "Edit Product";
}

<h2>@ViewData["Title"]</h2>

<div class="row">
    <div class="col-md-8">
        <form asp-action="Save" method="post" enctype="multipart/form-data">
            
            <!-- Anti-forgery token (automatically included) -->
            
            <!-- Hidden field for ID (edit mode) -->
            @if (Model.Id > 0)
            {
                <input type="hidden" asp-for="Id" />
            }
            
            <!-- Validation Summary -->
            <div asp-validation-summary="ModelOnly" class="alert alert-danger"></div>
            
            <!-- Form Fields -->
            <div class="mb-3">
                <label asp-for="Name" class="form-label"></label>
                <input asp-for="Name" class="form-control" />
                <span asp-validation-for="Name" class="text-danger"></span>
            </div>
            
            <div class="mb-3">
                <label asp-for="Description" class="form-label"></label>
                <textarea asp-for="Description" class="form-control" rows="4"></textarea>
                <span asp-validation-for="Description" class="text-danger"></span>
            </div>
            
            <div class="row">
                <div class="col-md-6">
                    <div class="mb-3">
                        <label asp-for="Price" class="form-label"></label>
                        <input asp-for="Price" class="form-control" type="number" step="0.01" />
                        <span asp-validation-for="Price" class="text-danger"></span>
                    </div>
                </div>
                
                <div class="col-md-6">
                    <div class="mb-3">
                        <label asp-for="Stock" class="form-label"></label>
                        <input asp-for="Stock" class="form-control" type="number" />
                        <span asp-validation-for="Stock" class="text-danger"></span>
                    </div>
                </div>
            </div>
            
            <!-- Dropdown -->
            <div class="mb-3">
                <label asp-for="CategoryId" class="form-label"></label>
                <select asp-for="CategoryId" class="form-select" asp-items="ViewBag.Categories">
                    <option value="">-- Select Category --</option>
                </select>
                <span asp-validation-for="CategoryId" class="text-danger"></span>
            </div>
            
            <!-- Checkbox -->
            <div class="mb-3 form-check">
                <input asp-for="IsActive" class="form-check-input" type="checkbox" />
                <label asp-for="IsActive" class="form-check-label"></label>
            </div>
            
            <!-- File Upload -->
            <div class="mb-3">
                <label for="imageFile" class="form-label">Product Image</label>
                <input type="file" name="imageFile" class="form-control" accept="image/*" />
            </div>
            
            <!-- Submit Buttons -->
            <div class="mb-3">
                <button type="submit" class="btn btn-primary">
                    <i class="fa fa-save"></i> Save
                </button>
                <a asp-action="Index" class="btn btn-secondary">
                    <i class="fa fa-times"></i> Cancel
                </a>
            </div>
        </form>
    </div>
    
    <!-- Preview/Help Section -->
    <div class="col-md-4">
        <div class="card">
            <div class="card-header">Help</div>
            <div class="card-body">
                <ul>
                    <li>Product name is required</li>
                    <li>Price must be greater than 0</li>
                    <li>Select appropriate category</li>
                </ul>
            </div>
        </div>
    </div>
</div>

@section Scripts {
    @{await Html.RenderPartialAsync("_ValidationScriptsPartial");}
    
    <script>
        // Custom validation or preview logic
        document.querySelector('input[type="file"]').addEventListener('change', function(e) {
            // Image preview logic
        });
    </script>
}
```

**Controller Actions**:
```csharp
// GET: Product/Create
public async Task<IActionResult> Create()
{
    ViewBag.Categories = new SelectList(await _categoryService.GetAllAsync(), "Id", "Name");
    return View(new Product());
}

// GET: Product/Edit/5
public async Task<IActionResult> Edit(int id)
{
    var product = await _productService.GetByIdAsync(id);
    if (product == null) return NotFound();
    
    ViewBag.Categories = new SelectList(await _categoryService.GetAllAsync(), "Id", "Name", product.CategoryId);
    return View("Create", product);  // Reuse Create view
}

// POST: Product/Save
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Save(Product product, IFormFile imageFile)
{
    if (ModelState.IsValid)
    {
        // Handle file upload
        if (imageFile != null)
        {
            product.ImagePath = await _fileService.UploadAsync(imageFile);
        }
        
        if (product.Id == 0)
            await _productService.CreateAsync(product);
        else
            await _productService.UpdateAsync(product);
        
        TempData["SuccessMessage"] = "Product saved successfully!";
        return RedirectToAction(nameof(Index));
    }
    
    ViewBag.Categories = new SelectList(await _categoryService.GetAllAsync(), "Id", "Name", product.CategoryId);
    return View("Create", product);
}
```

---

### Pattern 3: Details View

**Purpose**: Display detailed information about a single entity.

**Model Type**: Single entity `T`

```razor
@model YourProject.Models.Product

@{
    ViewData["Title"] = "Product Details";
}

<h2>@Model.Name</h2>

<div class="row">
    <div class="col-md-8">
        <div class="card">
            <div class="card-header">
                <h4>Product Information</h4>
            </div>
            <div class="card-body">
                <dl class="row">
                    <dt class="col-sm-3">ID</dt>
                    <dd class="col-sm-9">@Model.Id</dd>
                    
                    <dt class="col-sm-3">Name</dt>
                    <dd class="col-sm-9">@Model.Name</dd>
                    
                    <dt class="col-sm-3">Description</dt>
                    <dd class="col-sm-9">@Model.Description</dd>
                    
                    <dt class="col-sm-3">Price</dt>
                    <dd class="col-sm-9">@Model.Price.ToString("C")</dd>
                    
                    <dt class="col-sm-3">Stock</dt>
                    <dd class="col-sm-9">
                        @Model.Stock
                        @if (Model.Stock < 10)
                        {
                            <span class="badge bg-warning">Low Stock</span>
                        }
                    </dd>
                    
                    <dt class="col-sm-3">Category</dt>
                    <dd class="col-sm-9">@Model.Category?.Name</dd>
                    
                    <dt class="col-sm-3">Status</dt>
                    <dd class="col-sm-9">
                        @if (Model.IsActive)
                        {
                            <span class="badge bg-success">Active</span>
                        }
                        else
                        {
                            <span class="badge bg-secondary">Inactive</span>
                        }
                    </dd>
                    
                    <dt class="col-sm-3">Created</dt>
                    <dd class="col-sm-9">
                        @Model.CreatedDate.ToString("MMM dd, yyyy")
                        <small class="text-muted">(@Model.CreatedDate.ToRelativeTime())</small>
                    </dd>
                </dl>
            </div>
        </div>
        
        <!-- Related Data (if any) -->
        @if (Model.Reviews != null && Model.Reviews.Any())
        {
            <div class="card mt-3">
                <div class="card-header">
                    <h5>Customer Reviews</h5>
                </div>
                <div class="card-body">
                    @foreach (var review in Model.Reviews)
                    {
                        <div class="border-bottom pb-2 mb-2">
                            <strong>@review.CustomerName</strong>
                            <span class="text-warning">@String.Join("", Enumerable.Repeat("★", review.Rating))</span>
                            <p>@review.Comment</p>
                        </div>
                    }
                </div>
            </div>
        }
    </div>
    
    <div class="col-md-4">
        <!-- Image -->
        @if (!string.IsNullOrEmpty(Model.ImagePath))
        {
            <img src="@Model.ImagePath" class="img-fluid rounded mb-3" alt="@Model.Name" />
        }
        
        <!-- Actions -->
        <div class="d-grid gap-2">
            <a asp-action="Edit" asp-route-id="@Model.Id" class="btn btn-warning">
                <i class="fa fa-edit"></i> Edit
            </a>
            <a asp-action="Delete" asp-route-id="@Model.Id" class="btn btn-danger">
                <i class="fa fa-trash"></i> Delete
            </a>
            <a asp-action="Index" class="btn btn-secondary">
                <i class="fa fa-arrow-left"></i> Back to List
            </a>
        </div>
    </div>
</div>
```

---

### Pattern 4: Delete Confirmation View

**Purpose**: Confirm before deleting an entity.

```razor
@model YourProject.Models.Product

@{
    ViewData["Title"] = "Delete Product";
}

<h2>Delete Confirmation</h2>

<div class="alert alert-danger">
    <h4>Are you sure you want to delete this product?</h4>
    <p>This action cannot be undone.</p>
</div>

<div class="card">
    <div class="card-body">
        <dl class="row">
            <dt class="col-sm-3">Name</dt>
            <dd class="col-sm-9">@Model.Name</dd>
            
            <dt class="col-sm-3">Price</dt>
            <dd class="col-sm-9">@Model.Price.ToString("C")</dd>
            
            <dt class="col-sm-3">Category</dt>
            <dd class="col-sm-9">@Model.Category?.Name</dd>
        </dl>
    </div>
</div>

<form asp-action="Delete" method="post" class="mt-3">
    <input type="hidden" asp-for="Id" />
    <button type="submit" class="btn btn-danger">
        <i class="fa fa-trash"></i> Confirm Delete
    </button>
    <a asp-action="Index" class="btn btn-secondary">Cancel</a>
</form>
```

---

### Pattern 5: Login View

**Purpose**: Authentication form.

```razor
@model YourProject.ViewModels.LoginViewModel

@{
    ViewData["Title"] = "Login";
    Layout = "_Layout";  // or a custom login layout
}

<div class="row justify-content-center">
    <div class="col-md-6 col-lg-4">
        <div class="card shadow">
            <div class="card-body">
                <h3 class="card-title text-center mb-4">Login</h3>
                
                <form asp-action="Login" asp-route-returnUrl="@ViewData["ReturnUrl"]" method="post">
                    <div asp-validation-summary="ModelOnly" class="alert alert-danger"></div>
                    
                    <div class="mb-3">
                        <label asp-for="Email" class="form-label"></label>
                        <input asp-for="Email" class="form-control" autofocus />
                        <span asp-validation-for="Email" class="text-danger"></span>
                    </div>
                    
                    <div class="mb-3">
                        <label asp-for="Password" class="form-label"></label>
                        <input asp-for="Password" type="password" class="form-control" />
                        <span asp-validation-for="Password" class="text-danger"></span>
                    </div>
                    
                    <div class="mb-3 form-check">
                        <input asp-for="RememberMe" class="form-check-input" />
                        <label asp-for="RememberMe" class="form-check-label"></label>
                    </div>
                    
                    <div class="d-grid">
                        <button type="submit" class="btn btn-primary">Login</button>
                    </div>
                </form>
                
                <hr />
                
                <div class="text-center">
                    <a asp-action="ForgotPassword">Forgot your password?</a>
                </div>
                <div class="text-center">
                    Don't have an account? <a asp-action="Register">Register</a>
                </div>
            </div>
        </div>
    </div>
</div>

@section Scripts {
    @{await Html.RenderPartialAsync("_ValidationScriptsPartial");}
}
```

---

## Data Passing Mechanisms

### 1. **Model (Strongly-Typed)**

**Best for**: Primary data being displayed/edited

```csharp
// Controller
public IActionResult Index()
{
    var products = _productService.GetAll();
    return View(products);  // Pass model
}

// View
@model IEnumerable<Product>
@foreach (var item in Model)
{
    <p>@item.Name</p>
}
```

**Pros**: Type-safe, IntelliSense support
**Cons**: One model per view

---

### 2. **ViewData**

**Best for**: Small amounts of data, dynamic keys

```csharp
// Controller
ViewData["Title"] = "Product List";
ViewData["TotalCount"] = products.Count;

// View
<h2>@ViewData["Title"]</h2>
<p>Total: @ViewData["TotalCount"]</p>
```

**Pros**: Simple, dictionary-based
**Cons**: No type safety, requires casting, prone to typos

---

### 3. **ViewBag**

**Best for**: Similar to ViewData but with dynamic syntax

```csharp
// Controller
ViewBag.Title = "Product List";
ViewBag.Categories = new SelectList(categories, "Id", "Name");

// View
<h2>@ViewBag.Title</h2>
<select asp-items="ViewBag.Categories"></select>
```

**Pros**: Dynamic, no casting needed
**Cons**: No IntelliSense, runtime errors if property doesn't exist

---

### 4. **TempData**

**Best for**: Passing data after redirect (Post-Redirect-Get pattern)

```csharp
// Controller
TempData["SuccessMessage"] = "Product created successfully!";
return RedirectToAction("Index");

// View (after redirect)
@if (TempData["SuccessMessage"] != null)
{
    <div class="alert alert-success">@TempData["SuccessMessage"]</div>
}
```

**Pros**: Survives redirects
**Cons**: Only available for current and next request

---

### 5. **ViewModel Pattern** (Recommended for Complex Views)

**Best for**: Views that need multiple data types

```csharp
// ViewModel
public class ProductIndexViewModel
{
    public IEnumerable<Product> Products { get; set; }
    public string SearchTerm { get; set; }
    public int TotalCount { get; set; }
    public List<Category> Categories { get; set; }
}

// Controller
public IActionResult Index(string search)
{
    var viewModel = new ProductIndexViewModel
    {
        Products = _productService.Search(search),
        SearchTerm = search,
        TotalCount = _productService.GetCount(),
        Categories = _categoryService.GetAll()
    };
    return View(viewModel);
}

// View
@model ProductIndexViewModel
<p>Found @Model.TotalCount products</p>
@foreach (var product in Model.Products)
{
    <p>@product.Name</p>
}
```

---

## Tag Helpers Reference

### Common Tag Helpers

```razor
<!-- Link to action -->
<a asp-controller="Product" asp-action="Index">Products</a>
<a asp-action="Details" asp-route-id="@item.Id">View</a>

<!-- Form tag -->
<form asp-controller="Product" asp-action="Create" method="post">
    <!-- Anti-forgery token automatically added -->
</form>

<!-- Input binding -->
<input asp-for="Name" class="form-control" />
<textarea asp-for="Description" class="form-control"></textarea>
<select asp-for="CategoryId" asp-items="ViewBag.Categories" class="form-select"></select>

<!-- Validation -->
<span asp-validation-for="Name" class="text-danger"></span>
<div asp-validation-summary="All" class="alert alert-danger"></div>

<!-- Label -->
<label asp-for="Name" class="form-label"></label>

<!-- Image tag -->
<img asp-append-version="true" src="~/images/logo.png" />

<!-- Environment-specific content -->
<environment include="Development">
    <link rel="stylesheet" href="~/css/site.css" />
</environment>
<environment exclude="Development">
    <link rel="stylesheet" href="~/css/site.min.css" asp-append-version="true" />
</environment>

<!-- Cache tag helper -->
<cache expires-after="@TimeSpan.FromMinutes(30)">
    <!-- Expensive operation result -->
</cache>
```

---

## Form Handling Patterns

### Basic Form Pattern

```razor
<form asp-action="Create" method="post">
    <div class="mb-3">
        <label asp-for="Name"></label>
        <input asp-for="Name" class="form-control" />
        <span asp-validation-for="Name" class="text-danger"></span>
    </div>
    
    <button type="submit" class="btn btn-primary">Submit</button>
</form>
```

### Form with File Upload

```razor
<form asp-action="Create" method="post" enctype="multipart/form-data">
    <div class="mb-3">
        <label for="file">Upload File</label>
        <input type="file" name="file" id="file" class="form-control" />
    </div>
    
    <button type="submit" class="btn btn-primary">Upload</button>
</form>
```

**Controller**:
```csharp
[HttpPost]
public async Task<IActionResult> Create(ProductViewModel model, IFormFile file)
{
    if (file != null)
    {
        var filePath = await _fileService.SaveFileAsync(file);
        model.FilePath = filePath;
    }
    // ...
}
```

### AJAX Form Pattern

```razor
<form id="ajaxForm">
    <input type="text" id="name" name="name" />
    <button type="submit">Submit</button>
</form>

<div id="result"></div>

@section Scripts {
    <script>
        $('#ajaxForm').on('submit', function(e) {
            e.preventDefault();
            
            $.ajax({
                url: '@Url.Action("Create", "Product")',
                type: 'POST',
                data: $(this).serialize(),
                success: function(response) {
                    $('#result').html('<div class="alert alert-success">Success!</div>');
                },
                error: function() {
                    $('#result').html('<div class="alert alert-danger">Error!</div>');
                }
            });
        });
    </script>
}
```

---

## Client-Side Integration

### Adding Bootstrap

In `_Layout.cshtml`:
```html
<head>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet">
</head>
<body>
    <!-- Content -->
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js"></script>
</body>
```

### Adding Font Awesome

```html
<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css">

<!-- Usage -->
<i class="fas fa-user"></i>
<i class="fas fa-edit"></i>
<i class="fas fa-trash"></i>
```

### jQuery Integration

```razor
@section Scripts {
    <script>
        $(document).ready(function() {
            // Initialize tooltips
            $('[data-bs-toggle="tooltip"]').tooltip();
            
            // Confirm delete
            $('.delete-btn').on('click', function(e) {
                if (!confirm('Are you sure?')) {
                    e.preventDefault();
                }
            });
            
            // Live search
            $('#searchInput').on('keyup', function() {
                var value = $(this).val().toLowerCase();
                $('#tableBody tr').filter(function() {
                    $(this).toggle($(this).text().toLowerCase().indexOf(value) > -1);
                });
            });
        });
    </script>
}
```

---

## Best Practices Checklist

### ✅ Structure
- [ ] Use `_ViewStart.cshtml` for default layout
- [ ] Use `_ViewImports.cshtml` for global imports
- [ ] Organize views by controller name
- [ ] Use partial views for reusable components
- [ ] Create ViewModels for complex views

### ✅ Models
- [ ] Use strongly-typed models (`@model`)
- [ ] Prefer `IEnumerable<T>` for list views
- [ ] Use ViewModels instead of mixing ViewData/ViewBag
- [ ] Keep business logic out of views

### ✅ Forms
- [ ] Use Tag Helpers instead of HTML Helpers
- [ ] Include validation on all input fields
- [ ] Use `_ValidationScriptsPartial` for client-side validation
- [ ] Anti-forgery tokens are automatic with `<form>` tag helper
- [ ] Use `enctype="multipart/form-data"` for file uploads

### ✅ Data Passing
- [ ] Use Model for primary data
- [ ] Use ViewData/ViewBag for supplementary data
- [ ] Use TempData for post-redirect messages
- [ ] Prefer ViewModels over excessive ViewBag usage

### ✅ UI/UX
- [ ] Make views responsive with Bootstrap
- [ ] Show loading indicators for async operations
- [ ] Display success/error messages with TempData
- [ ] Confirm destructive actions (delete)
- [ ] Provide navigation breadcrumbs

### ✅ Security
- [ ] Never display raw HTML unless necessary (`@Html.Raw`)
- [ ] All user input is automatically HTML encoded
- [ ] Use `asp-append-version` for cache busting
- [ ] Validate on both client and server side

### ✅ Performance
- [ ] Use caching for expensive operations
- [ ] Lazy load images
- [ ] Minimize JavaScript in views
- [ ] Use CDNs for libraries

---

## Common Patterns Summary

| Pattern | When to Use | Example |
|---------|-------------|---------|
| Index/List | Display collection | Product list, User list |
| Create | Add new entity | New product form |
| Edit | Modify existing entity | Edit product form |
| Details | Show single entity | Product details page |
| Delete | Confirm deletion | Delete confirmation |
| Login | Authentication | User login form |
| Search | Filter data | Search products |
| Dashboard | Overview/Summary | Admin dashboard |

---

## Quick Reference: View to Controller Flow

```
1. User interacts with View (clicks link/submits form)
2. Browser sends HTTP request to Controller
3. Controller action executes:
   - Validates input (ModelState)
   - Calls Service layer for business logic
   - Prepares data (Model, ViewData, ViewBag)
   - Returns View/Redirect
4. View receives data and renders
5. Razor engine processes .cshtml
6. HTML sent to browser
7. Browser displays page
```

---

## Sample Checklist for New Feature

When implementing a new feature with views:

1. **Plan**
   - [ ] Identify required views (CRUD?)
   - [ ] Design ViewModel if needed
   - [ ] Plan navigation flow

2. **Create Views**
   - [ ] Create Index view (list)
   - [ ] Create Create/Edit view (form)
   - [ ] Create Details view (if needed)
   - [ ] Create Delete confirmation (if needed)

3. **Implement Forms**
   - [ ] Add form fields with tag helpers
   - [ ] Add validation attributes on model
   - [ ] Include validation scripts
   - [ ] Test client-side validation
   - [ ] Test server-side validation

4. **Add Navigation**
   - [ ] Add links in navigation bar
   - [ ] Add action buttons (Create, Edit, Delete)
   - [ ] Add breadcrumbs if applicable

5. **Polish**
   - [ ] Add success/error messages with TempData
   - [ ] Style with Bootstrap classes
   - [ ] Test responsive design
   - [ ] Add icons for better UX

6. **Test**
   - [ ] Test all CRUD operations
   - [ ] Test validation (empty fields, invalid data)
   - [ ] Test navigation between views
   - [ ] Test on mobile devices

---

## Conclusion

This guide provides a comprehensive reference for implementing views in ASP.NET Core MVC applications. Follow these patterns and best practices to create maintainable, user-friendly interfaces.

**Key Takeaways**:
- Use strong typing with `@model`
- Follow MVC conventions
- Use Tag Helpers for cleaner code
- Keep business logic in controllers/services
- Use ViewModels for complex views
- Implement proper validation
- Make UI responsive with Bootstrap

---

**Generated**: March 13, 2026  
**For**: ASP.NET Core MVC Projects  
**Version**: .NET 8.0+
