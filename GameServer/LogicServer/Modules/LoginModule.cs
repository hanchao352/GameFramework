using System.Threading.Tasks;
using Common.Network;
using Data.Repositories;
using Game;
using Google.Protobuf;


namespace LogicServer.Modules
{
    public class LoginModule
    {
        private readonly MessageDispatcher _disp;
        private readonly IUserRepository _users;

        public LoginModule(MessageDispatcher disp, IUserRepository users)
       {
            _disp  = disp;
            _users = users;
        }

        public void Init()
        {
            _disp.RegisterHandler<LoginReq>( OnLoginReq);
        }

        private async Task OnLoginReq(Session session, LoginReq req)
        {
            var res = new LoginRes();
            var user = await _users.GetByAccountAsync(req.Account);
            if (user == null)
            {
                user = new Data.Models.User { Account=req.Account, Password=req.Password, PlayerId=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };
                await _users.CreateAsync(user);
                res.Success  = true;
                res.PlayerId = user.PlayerId;
            }
            else if (user.Password != req.Password)
            {
                res.Success = false;
                res.Message = "密码错误";
            }
            else
            {
                res.Success  = true;
                res.PlayerId = user.PlayerId;
            }
            var packet = PacketHelper.CreatePacket(1002, res.ToByteArray());
            await session.SendAsync(packet);
        }
    }
}
