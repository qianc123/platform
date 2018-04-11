using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.AutoMapper;
using Abp.Domain.Repositories;
using Platform.Authorization;
using Platform.Users.Dto;
using Microsoft.AspNet.Identity;
using Abp.UI;
using Abp.Threading;

namespace Platform.Users
{
    /* THIS IS JUST A SAMPLE. */
    [AbpAuthorize(PermissionNames.Pages_Users)]
    public class UserAppService : PlatformAppServiceBase, IUserAppService
    {
        private readonly IRepository<User, long> _userRepository;
        private readonly IPermissionManager _permissionManager;
        private readonly LogInManager _loginManager;

        public UserAppService(IRepository<User, long> userRepository, IPermissionManager permissionManager,
            LogInManager loginManager)
        {
            _userRepository = userRepository;
            _permissionManager = permissionManager;
            _loginManager = loginManager;
        }

        public async Task ProhibitPermission(ProhibitPermissionInput input)
        {
            var user = await UserManager.GetUserByIdAsync(input.UserId);
            var permission = _permissionManager.GetPermission(input.PermissionName);

            await UserManager.ProhibitPermissionAsync(user, permission);
        }

        //Example for primitive method parameters.
        public async Task RemoveFromRole(long userId, string roleName)
        {
            CheckErrors(await UserManager.RemoveFromRoleAsync(userId, roleName));
        }

        public async Task<ListResultDto<UserListDto>> GetUsers()
        {
            var users = await _userRepository.GetAllListAsync();

            return new ListResultDto<UserListDto>(
                users.MapTo<List<UserListDto>>()
                );
        }

        public async Task CreateUser(CreateUserInput input)
        {
            var user = input.MapTo<User>();

            user.TenantId = AbpSession.TenantId;
            user.Password = new PasswordHasher().HashPassword(input.Password);
            user.IsEmailConfirmed = true;

            CheckErrors(await UserManager.CreateAsync(user));
        }

        public async Task UpdatePwd(UpdatePwdInput input)
        {
            //��鴫�����
            if (!AbpSession.TenantId.HasValue) throw new UserFriendlyException("����Ȩ���ʸ�ϵͳ��");
            if (!input.Id.HasValue) throw new UserFriendlyException("����Id��������ȷ��");
            if (string.IsNullOrEmpty(input.Password)) throw new UserFriendlyException("����Password��������ȷ��");
            if (string.IsNullOrEmpty(input.OldPassword)) throw new UserFriendlyException("����OldPassword��������ȷ��");

            //��ȡ��Ҫ�޸ĵĶ���
            var customer = await _userRepository.FirstOrDefaultAsync(x => x.Id == input.Id.Value);
            if (customer == null) throw new UserFriendlyException("��ǰ��¼�����ڣ�");

            //�޸�����
            if (!string.IsNullOrEmpty(input.Password))
            {
                var user = AsyncHelper.RunSync(() => UserManager.GetUserByIdAsync(AbpSession.UserId ?? 0));
                if (user == null) throw new UserFriendlyException("�����µ�¼��");

                var tenant = await TenantManager.GetByIdAsync(AbpSession.TenantId.Value);
                var loginResult = await _loginManager.LoginAsync(user.UserName, input.OldPassword, tenant?.TenancyName);
                if (loginResult.Result != AbpLoginResultType.Success)
                {
                    throw new UserFriendlyException("ԭ�������");
                }

                customer.Password = new PasswordHasher().HashPassword(input.Password);
            }

            //ִ���޸����ݷ���
            await _userRepository.UpdateAsync(customer);
        }
    }
}