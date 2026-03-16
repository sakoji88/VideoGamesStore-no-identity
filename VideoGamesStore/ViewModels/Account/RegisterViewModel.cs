using System.ComponentModel.DataAnnotations;

namespace VideoGamesStore.ViewModels.Account;

public class RegisterViewModel
{
    [Display(Name = "Логин")]
    [Required(ErrorMessage = "Укажите логин")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Логин должен быть от 3 до 50 символов")]
    [RegularExpression(@"^[a-zA-Z0-9_.-]+$", ErrorMessage = "Используйте только буквы, цифры и символы _ . -")]
    public string Username { get; set; } = string.Empty;

    [Display(Name = "Email")]
    [Required(ErrorMessage = "Укажите email")]
    [EmailAddress(ErrorMessage = "Введите email в формате name@example.com")]
    [StringLength(100, ErrorMessage = "Email не должен превышать 100 символов")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Пароль")]
    [Required(ErrorMessage = "Введите пароль")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен быть от 6 до 100 символов")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Повторите пароль")]
    [Required(ErrorMessage = "Подтвердите пароль")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Пароли не совпадают")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
