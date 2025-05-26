namespace FastTechFood.Domain.Enums
{
    public enum UserType { Customer, Employee, KitchenStaff, Manager }

    public enum ProductType { Sandwich, Dessert, Drink, Combok }

    public enum OrderStatus { Pending, Accepted, Rejected, Canceled, InPreparation, Ready, Delivered }

    public enum DeliveryType { Counter, DriveThru, Delivery }
}
