using System.ComponentModel.DataAnnotations;

namespace VideoGamesStore.ViewModels.Account;

public class LoginViewModel
{
    [Required(ErrorMessage = "Введите логин или email")]
    [StringLength(100)]
    public string Login { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите пароль")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;
}
