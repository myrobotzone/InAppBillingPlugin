using Plugin.InAppBilling.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Services.Store;

namespace Plugin.InAppBilling
{
	/// <summary>
	/// Implementation for Feature
	/// </summary>
	public class InAppBillingImplementation : BaseInAppBilling
	{
		/// <summary>
		/// Default constructor
		/// </summary>
		public InAppBillingImplementation()
		{
		}

		/// <summary>
		/// Gets or sets if in testing mode. Only for UWP
		/// </summary>
		public override bool InTestingMode { get; set; }


		/// <summary>
		/// Connect to billing service
		/// </summary>
		/// <returns>If Success</returns>
		public override Task<bool> ConnectAsync() => Task.FromResult(true);

		/// <summary>
		/// Disconnect from the billing service
		/// </summary>
		/// <returns>Task to disconnect</returns>
		public override Task DisconnectAsync() => Task.CompletedTask;

		/// <summary>
		/// Gets product information
		/// </summary>
		/// <param name="itemType">Type of item</param>
		/// <param name="productIds">Product Ids</param>
		/// <returns></returns>
		public override async Task<IEnumerable<InAppBillingProduct>> GetProductInfoAsync(ItemType itemType, params string[] productIds)
		{
			string[] productKinds = {
				"Application", "Game", "Consumable", "UnmanagedConsumable", "Durable"
			};

			var context = StoreContext.GetDefault();
			var productsQuery = await context.GetStoreProductsAsync(productKinds, productIds);

			return productsQuery.Products.Select(p => new InAppBillingProduct()
			{
				ProductId = p.Value.StoreId,
				Name = p.Value.Title,
				Description = p.Value.Description,
				LocalizedPrice = p.Value.Price.FormattedRecurrencePrice,
				CurrencyCode = p.Value.Price.CurrencyCode
			});
		}

		/// <summary>
		/// Get all current purchase for a specific product type.
		/// </summary>
		/// <param name="itemType">Type of product</param>
		/// <param name="verifyPurchase">Verify purchase implementation</param>
		/// <returns>The current purchases</returns>
		public override async Task<IEnumerable<InAppBillingPurchase>> GetPurchasesAsync(ItemType itemType, IInAppBillingVerifyPurchase verifyPurchase = null)
		{
			// https://docs.microsoft.com/en-us/windows/uwp/monetize/query-for-products
			// https://docs.microsoft.com/en-us/windows/uwp/monetize/enable-subscription-add-ons-for-your-app#code-examples

			var context = StoreContext.GetDefault();
			var appLicense = await context.GetAppLicenseAsync();
			var purchases = new List<InAppBillingPurchase>()
			{
				new InAppBillingPurchase
				{
					ProductId = appLicense.SkuStoreId,
					ExpirationDate = appLicense.ExpirationDate,
					State = appLicense.IsActive ? PurchaseState.Purchased : PurchaseState.Unknown,
				}
			};
			purchases.AddRange(appLicense.AddOnLicenses.Select(l => new InAppBillingPurchase
			{
				ProductId = l.Value.SkuStoreId,
				ExpirationDate = appLicense.ExpirationDate,
				State = l.Value.IsActive ? PurchaseState.Purchased : PurchaseState.Unknown
			}));
			return purchases;
		}

		/// <summary>
		/// Purchase a specific product or subscription
		/// </summary>
		/// <param name="productId">Sku or ID of product</param>
		/// <param name="itemType">Type of product being requested</param>
		/// <param name="payload">Developer specific payload</param>
		/// <param name="verifyPurchase">Verify purchase implementation</param>
		/// <returns></returns>
		/// <exception cref="InAppBillingPurchaseException">If an error occurs during processing</exception>
		public override async Task<InAppBillingPurchase> PurchaseAsync(string productId, ItemType itemType, string payload, IInAppBillingVerifyPurchase verifyPurchase = null)
		{
			var storeContext = StoreContext.GetDefault();
			// Get purchase result from store or simulator
			var purchaseResult = await storeContext.RequestPurchaseAsync(productId);

			return new InAppBillingPurchase
			{
				State = Map(purchaseResult.Status),
			};
		}

		/// <summary>
		/// Consume a purchase with a purchase token.
		/// </summary>
		/// <param name="productId">Id or Sku of product</param>
		/// <param name="purchaseToken">Original Purchase Token</param>
		/// <returns>If consumed successful</returns>
		/// <exception cref="InAppBillingPurchaseException">If an error occures during processing</exception>
		public override async Task<InAppBillingPurchase> ConsumePurchaseAsync(string productId, string purchaseToken)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Consume a purchase
		/// </summary>
		/// <param name="productId">Id/Sku of the product</param>
		/// <param name="payload">Developer specific payload of original purchase</param>
		/// <param name="itemType">Type of product being consumed.</param>
		/// <param name="verifyPurchase">Verify Purchase implementation</param>
		/// <returns>If consumed successful</returns>
		/// <exception cref="InAppBillingPurchaseException">If an error occures during processing</exception>
		public override async Task<InAppBillingPurchase> ConsumePurchaseAsync(string productId, ItemType itemType, string payload, IInAppBillingVerifyPurchase verifyPurchase = null)
		{
			throw new NotImplementedException();
		}

		private PurchaseState Map(StorePurchaseStatus status)
		{
			switch (status)
			{
				case StorePurchaseStatus.Succeeded:
					return PurchaseState.Purchased;
				case StorePurchaseStatus.AlreadyPurchased:
					return PurchaseState.Restored;
				case StorePurchaseStatus.NotPurchased:
					return PurchaseState.Failed;
				case StorePurchaseStatus.NetworkError:
					return PurchaseState.Unknown;
				case StorePurchaseStatus.ServerError:
					return PurchaseState.Unknown;
			}
			return PurchaseState.Unknown;
		}
	}
}
