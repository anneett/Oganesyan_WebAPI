using Microsoft.AspNetCore.Identity.Data;
using System.ComponentModel.DataAnnotations;

namespace Oganesyan_WebAPI.Models
{
    public enum UserRole
    {
        Admin,
        User
    }
    public class User
    {
        public int Id { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }

        [Required]
        [EnumDataType(typeof(UserRole))]
        public UserRole Role { get; set; }

        public bool IsAdmin()
        {
            return Role == UserRole.Admin;
        }

        // Авторизация: свериться, есть ли такой логин в бд, потом проверка пароля

        // +-: просмотр профиля/просмотреть статистику


        //public bool SuccessAutorization(string login, string password)
        //{
        //    if (login == Login && password == Password)
        //    {
        //        return true;
        //    }
        //    return false;
        //}

        //Statistics
    }
}
