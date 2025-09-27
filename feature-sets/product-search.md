# Product Search Feature Set

## Overview
This document defines a simple product search system where agents can search for home improvement products across Home Depot, Rona, and Amazon, with results displayed in a unified list containing affiliate links for commission earning.

## Current State
- 🔄 **Phase 1 Pending**: Basic web scraping and product search implementation

## Requirements

### Product Search Flow
1. **Agent Query**: Agent enters search terms for products
2. **Multi-Site Scraping**: System scrapes Home Depot, Rona, and Amazon concurrently
3. **Result Display**: Present aggregated results in a simple list format with pagination
4. **Affiliate Links**: Include referral codes in product URLs for commission

### Supported Retailers
- **Home Depot**: `homedepot.com` or `homedepot.ca`
- **Rona**: `rona.ca`
- **Amazon**: `amazon.ca`

## Technical Implementation

### Web Scraping Best Practices
- **Rate Limiting**: 3-5 second delays between requests per site
- **User-Agent Rotation**: Rotate browser signatures to avoid detection
- **Request Headers**: Vary Accept-Language, Accept-Encoding, referer headers
- **Error Handling**: Graceful degradation when sites are unavailable
- **Concurrent Execution**: Scrape all three sites simultaneously for faster results
- **Pagination Support**: Handle multiple pages of results from each retailer

### Data Extraction
**Home Depot:**
- Search: `https://www.homedepot.ca/search?q={query}&page={pageNum}`
- Extract: title, price, image URL, product URL
- Handle pagination links for additional results

**Rona:**
- Search: `https://www.rona.ca/en/search/{query}?page={pageNum}`
- Extract: title, price, image URL, product URL
- Navigate through multiple result pages

**Amazon:**
- Search: `https://www.amazon.ca/s?k={query}&page={pageNum}`
- Extract: title, price, image URL, product URL
- Handle Amazon's pagination structure

### Affiliate Link Integration
- Add appropriate referral parameters to each retailer's URLs
- Maintain original product functionality while earning commission

## API Endpoint

### GET /api/products/v1/search
**Request:**
```
GET /api/products/v1/search?query=cordless+drill&page=1&pageSize=20
```

**Query Parameters:**
- `query` (required): Search terms
- `page` (optional): Page number, defaults to 1
- `pageSize` (optional): Results per page, defaults to 20, max 50

**Response:**
```json
{
  "query": "cordless drill",
  "page": 1,
  "pageSize": 20,
  "totalResults": 156,
  "totalPages": 8,
  "hasNextPage": true,
  "hasPreviousPage": false,
  "results": [
    {
      "retailer": "homedepot",
      "title": "DEWALT 20V Max Cordless Drill",
      "price": "129.99",
      "currency": "CAD",
      "imageUrl": "https://images.homedepot.ca/productimages/...",
      "productUrl": "https://www.homedepot.ca/product/...?ref=realtor_app"
    },
    {
      "retailer": "rona",
      "title": "Milwaukee M18 Cordless Drill Kit",
      "price": "149.99",
      "currency": "CAD",
      "imageUrl": "https://media.rona.ca/...",
      "productUrl": "https://www.rona.ca/en/product/...?ref=realtor_app"
    },
    {
      "retailer": "amazon",
      "title": "BLACK+DECKER 20V MAX Cordless Drill",
      "price": "89.99",
      "currency": "CAD",
      "imageUrl": "https://m.media-amazon.com/...",
      "productUrl": "https://www.amazon.ca/dp/...?tag=realtor_app"
    }
  ]
}
```

## Database Schema

### ProductSearches Table (Optional - for basic logging)
- `search_id` (primary key)
- `agent_id` (foreign key to agents)
- `query` (search terms)
- `page` (requested page number)
- `result_count` (number of products found)
- `created_at`

## Service Architecture

### ProductService
**Responsibilities:**
- Main orchestrator for product search functionality
- Handles search requests and pagination logic
- Coordinates results from multiple retailers
- Manages response formatting and error handling
- Implements caching strategy for performance
- Generates affiliate links with proper referral codes

**Key Methods:**
- `SearchProductsAsync(query, page, pageSize)` - Main search entry point
- `AggregateResultsAsync(scrapingResults)` - Combines and sorts results from all retailers
- `ApplyPaginationAsync(results, page, pageSize)` - Handles pagination logic
- `GenerateAffiliateLinksAsync(products)` - Adds referral parameters to product URLs

### ScrapingService
**Responsibilities:**
- Handles HTTP requests and anti-detection measures
- Manages rate limiting and request throttling
- Implements user-agent rotation and header variation
- Returns raw HTML content from retailer websites
- Handles proxy rotation and IP management
- Implements retry logic and circuit breakers

**Key Methods:**
- `ScrapeRetailerAsync(retailer, query, pageNumber)` - Scrape single retailer
- `ScrapeAllRetailersAsync(query, pageNumber)` - Concurrent scraping of all retailers
- `ExecuteRequestAsync(url, headers)` - Core HTTP request with anti-detection
- `HandleRateLimitingAsync(retailer)` - Manage request timing per retailer

### ParsingService
**Responsibilities:**
- Extracts structured product data from raw HTML
- Implements retailer-specific parsing logic
- Normalizes data into consistent format across retailers
- Handles parsing errors and data validation
- Cleans and sanitizes extracted product information

**Key Methods:**
- `ParseHomeDepotAsync(html)` - Extract Home Depot product data
- `ParseRonaAsync(html)` - Extract Rona product data
- `ParseAmazonAsync(html)` - Extract Amazon product data
- `NormalizeProductDataAsync(rawProducts, retailer)` - Standardize product format
- `ValidateProductDataAsync(products)` - Ensure data quality and completeness

## Implementation Details

### Service Pipeline
```
ProductService.SearchProductsAsync()
    ↓
ScrapingService.ScrapeAllRetailersAsync()
    ↓ (returns raw HTML)
ParsingService.ParseRetailerData()
    ↓ (returns structured products)
ProductService.AggregateResultsAsync()
    ↓ (applies pagination, generates affiliate links)
Return paginated response
```

### Pagination Strategy
- **Result Aggregation**: ProductService collects all parsed results, then applies pagination
- **Page Calculation**: Determine how many pages to scrape from each site based on requested page
- **Result Mixing**: Interleave results from different retailers for variety
- **Performance**: Cache results temporarily to avoid re-scraping for subsequent pages

### Error Handling Flow
- **ScrapingService**: Handle HTTP errors, timeouts, rate limiting
- **ParsingService**: Handle malformed HTML, missing elements, data validation
- **ProductService**: Aggregate errors, provide graceful degradation, return partial results

### Dependency Injection Setup
```csharp
// In Program.cs or DI container
services.AddScoped<IProductService, ProductService>();
services.AddScoped<IScrapingService, ScrapingService>();
services.AddScoped<IParsingService, ParsingService>();
```

### Controller
- GET endpoint that accepts query, page, and pageSize parameters
- Validates pagination parameters (page >= 1, pageSize <= 50)
- Calls ProductService.SearchProductsAsync() and returns results
- Basic input validation and sanitization

### Frontend Display
- Simple list/grid layout showing product cards with pagination controls
- Page navigation (Previous/Next buttons, page numbers)
- Each card displays: image, title, price, retailer logo
- Click opens product URL in new tab (with affiliate link)

## Success Criteria
- **Response Time**: Under 5 seconds for typical searches
- **Data Quality**: Successfully extract core product info from all three sites
- **Reliability**: Handle site unavailability gracefully
- **Affiliate Integration**: Properly formatted referral links for commission tracking
- **Pagination Performance**: Smooth navigation between pages without re-scraping when possible