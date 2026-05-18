using System.Threading.Tasks;

public interface IShopController : IController
{
    Task LoadItems();
    Task PurchaseItem(string itemId);
    void GenerateShopCards();
}
