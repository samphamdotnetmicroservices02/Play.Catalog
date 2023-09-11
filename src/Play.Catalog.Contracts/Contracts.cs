using System;

namespace Play.Catalog.Contracts;

/*
* One thing to remember here is that we don't need to define every single property of the catalog item object in these events.
*  Only we need to define here the properties that are of interest of our consumers
*/
public record CatalogItemCreated(Guid ItemId, string Name, string Description, decimal Price);

public record CatalogItemUpdated(Guid ItemId, string Name, string Description, decimal Price);

public record CatalogItemDeleted(Guid ItemId);