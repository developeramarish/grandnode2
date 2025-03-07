﻿using FluentValidation;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Models.Catalog;

namespace Grand.Web.Admin.Validators.Catalog;

public class AddProductSpecificationAttributeModelValidator : BaseStoreAccessValidator<ProductModel.AddProductSpecificationAttributeModel, Product>
{
    private readonly IProductService _productService;
    public AddProductSpecificationAttributeModelValidator(
        IEnumerable<IValidatorConsumer<ProductModel.AddProductSpecificationAttributeModel>> validators,
        ITranslationService translationService, IProductService productService, IContextAccessor contextAccessor,
        ISpecificationAttributeService specificationAttributeService)
        : base(validators, translationService, contextAccessor)
    {
        _productService = productService;

        RuleFor(x => x).MustAsync(async (x, _, _) =>
        {
            if (x.AttributeTypeId == SpecificationAttributeType.Option)
            {
                if (string.IsNullOrEmpty(x.SpecificationAttributeId))
                    return false;
                if (string.IsNullOrEmpty(x.SpecificationAttributeOptionId))
                    return false;

                var specification =
                    await specificationAttributeService.GetSpecificationAttributeById(x.SpecificationAttributeId);
                if (specification == null)
                    return false;

                if (specification.SpecificationAttributeOptions.FirstOrDefault(z =>
                        z.Id == x.SpecificationAttributeOptionId) == null)
                    return false;

                return true;
            }

            if (string.IsNullOrEmpty(x.CustomValue))
                return false;

            return true;
        }).WithMessage(translationService.GetResource("Admin.Catalog.Products.SpecificationAttributes.Validate"));
    }
    protected override async Task<Product> GetEntity(ProductModel.AddProductSpecificationAttributeModel model)
    {
        return await _productService.GetProductById(model.ProductId);
    }

    protected override string GetPermissionsResourceKey => "Admin.Catalog.Products.Permissions";
}