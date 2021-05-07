namespace PostgreApi.Models
{
    public class UserWithToken : User
    {

        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }


        public UserWithToken(User user)
        {
            this.Id = user.Id;
            this.EmailAddress = user.EmailAddress;
            this.FirstName = user.FirstName;
            this.MiddleName = user.MiddleName;
            this.LastName = user.LastName;
            this.HireDate = user.HireDate;
            this.ImgURL = user.ImgURL;
            this.Role = user.Role;
        }
    }
}