using System.ComponentModel.DataAnnotations;

namespace VideoGamesStore.ViewModels.Account;

public class LoginViewModel
{
    [Display(Name = "Логин или email")]
    [Required(ErrorMessage = "Введите логин или email")]
    [StringLength(100, ErrorMessage = "Поле не должно превышать 100 символов")]
    public string Login { get; set; } = string.Empty;

    [Display(Name = "Пароль")]
    [Required(ErrorMessage = "Введите пароль")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен быть от 6 до 100 символов")]
    public string Password { get; set; } = string.Empty;
}
