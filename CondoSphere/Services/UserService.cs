using CondoSphere.Data;
using CondoSphere.Data.Interfaces;

namespace CondoSphere.Services
{
    public class UserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<User> AuthenticateAsync(string email, string password)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null || !VerifyPassword(password, user.PasswordHash))
                return null;

            return user;
        }

        private bool VerifyPassword(string input, string hash)
        {
            // Password checking logic
            
            return true; 
        }
    }

}
