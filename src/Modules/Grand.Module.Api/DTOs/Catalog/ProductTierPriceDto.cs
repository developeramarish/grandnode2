﻿using Grand.Module.Api.Models;

namespace Grand.Module.Api.DTOs.Catalog;

public class ProductTierPriceDto : BaseApiEntityModel
{
    public string StoreId { get; set; }
    public string CustomerGroupId { get; set; }
    public int Quantity { get; set; }
    public double Price { get; set; }
    public DateTime? StartDateTimeUtc { get; set; }
    public DateTime? EndDateTimeUtc { get; set; }
}