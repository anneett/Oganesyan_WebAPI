using Microsoft.AspNetCore.Identity.Data;

namespace Oganesyan_WebAPI.Models
{
    public enum UserRole
    {
        User,
        Admin
    }
    public class User
    {
        public int Id { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public UserRole Role { get; set; }

        public bool IsAdmin()
        {
            return Role == UserRole.Admin;
        }

        // Авторизация: свериться, есть ли такой логин в бд, потом проверка пароля

        // Регистрация: добавление в бд пользователя

        // Сделать пользователя админом

        // +-: просмотр профиля/просмотреть статистику


        //public bool SuccessAutorization(string login, string password)
        //{
        //    if (login == Login && password == Password)
        //    {
        //        return true;
        //    }
        //}

        //Statistics
    }
}
