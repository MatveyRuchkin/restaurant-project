using AutoMapper;
using RestaurantAPI.DTOs;
using RestaurantAPI.Models;

namespace RestaurantAPI.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Dish, DishReadDto>()
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name));
        CreateMap<DishCreateDto, Dish>();
        CreateMap<DishUpdateDto, Dish>();

        CreateMap<Category, CategoryReadDto>();
        CreateMap<CategoryCreateDto, Category>();
        CreateMap<CategoryUpdateDto, Category>();

        CreateMap<User, UserReadDto>()
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role.Name));
        CreateMap<UserCreateDto, User>();
        CreateMap<UserUpdateDto, User>();

        CreateMap<Order, OrderReadDto>()
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.User.Username))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt));
        CreateMap<OrderCreateDto, Order>();
        CreateMap<OrderUpdateDto, Order>();

        CreateMap<OrderItem, OrderItemReadDto>()
            .ForMember(dest => dest.DishName, opt => opt.MapFrom(src => src.Dish.Name));
        CreateMap<OrderItemCreateDto, OrderItem>();

        CreateMap<Cart, CartReadDto>();
        CreateMap<CartItem, CartItemReadDto>()
            .ForMember(dest => dest.DishName, opt => opt.MapFrom(src => src.Dish.Name))
            .ForMember(dest => dest.DishPrice, opt => opt.MapFrom(src => src.Dish.Price))
            .ForMember(dest => dest.Subtotal, opt => opt.MapFrom(src => src.Dish.Price * src.Quantity));
        CreateMap<CartItemAddDto, CartItem>();
        CreateMap<CartItemUpdateDto, CartItem>();

        CreateMap<Menu, MenuReadDto>();
        CreateMap<MenuCreateDto, Menu>();
        CreateMap<MenuUpdateDto, Menu>();

        CreateMap<Ingredient, IngredientReadDto>();
        CreateMap<IngredientCreateDto, Ingredient>();
        CreateMap<IngredientUpdateDto, Ingredient>();

        CreateMap<Role, RoleReadDto>();
        CreateMap<RoleCreateDto, Role>();
        CreateMap<RoleUpdateDto, Role>();
    }
}

