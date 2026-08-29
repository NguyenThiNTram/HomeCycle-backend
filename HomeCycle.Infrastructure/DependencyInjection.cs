using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Agreements;
using HomeCycle.Application.Interfaces.Externals;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories;
using HomeCycle.Application.Interfaces.Repositories.Agreements;
using HomeCycle.Application.Interfaces.Repositories.Appointments;
using HomeCycle.Application.Interfaces.Repositories.Banks;
using HomeCycle.Application.Interfaces.Repositories.Carts;
using HomeCycle.Application.Interfaces.Repositories.Media;
using HomeCycle.Application.Interfaces.Repositories.Offers;
using HomeCycle.Application.Interfaces.Repositories.Orders;
using HomeCycle.Application.Interfaces.Repositories.Payments;
using HomeCycle.Application.Interfaces.Repositories.Posts;
using HomeCycle.Application.Interfaces.Repositories.Products;
using HomeCycle.Application.Interfaces.Repositories.Profiles;
using HomeCycle.Application.Interfaces.Repositories.Reviews;
using HomeCycle.Application.Interfaces.Repositories.Shipments;
using HomeCycle.Application.Interfaces.Repositories.Users;
using HomeCycle.Application.Interfaces.Repositories.Wallets;
using HomeCycle.Application.Interfaces.Security;
using HomeCycle.Application.Interfaces.Services.Agreements;
using HomeCycle.Application.Interfaces.Services.Auths;
using HomeCycle.Application.Interfaces.Services.Carts;
using HomeCycle.Application.Interfaces.Services.Externals;
using HomeCycle.Application.Interfaces.Services.Moderators;
using HomeCycle.Application.Interfaces.Services.Negotiates;
using HomeCycle.Application.Interfaces.Services.Offers;
using HomeCycle.Application.Interfaces.Services.Payments;
using HomeCycle.Application.Interfaces.Services.Posts;
using HomeCycle.Application.Interfaces.Services.Products;
using HomeCycle.Application.Interfaces.Services.Profiles;
using HomeCycle.Application.Interfaces.Services.Reviews;
using HomeCycle.Application.Interfaces.Services.Users;
using HomeCycle.Application.Mappings;
using HomeCycle.Application.Services.Agreements;
using HomeCycle.Application.Services.Auths;
using HomeCycle.Application.Services.Carts;
using HomeCycle.Application.Services.Moderators;
using HomeCycle.Application.Services.Negotiates;
using HomeCycle.Application.Services.Offers;
using HomeCycle.Application.Services.Payments;
using HomeCycle.Application.Services.Personals;
using HomeCycle.Application.Services.Posts;
using HomeCycle.Application.Services.Products;
using HomeCycle.Application.Services.Profiles;
using HomeCycle.Application.Services.Reviews;
using HomeCycle.Application.Validations.Agreements;
using HomeCycle.Application.Validations.Auths;
using HomeCycle.Application.Validations.Users;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Externals;
using HomeCycle.Infrastructure.Externals.GHN;
using HomeCycle.Infrastructure.Repositories.Agreements;
using HomeCycle.Infrastructure.Repositories.Appointments;
using HomeCycle.Infrastructure.Repositories.Banks;
using HomeCycle.Infrastructure.Repositories.Carts;
using HomeCycle.Infrastructure.Repositories.Offers;
using HomeCycle.Infrastructure.Repositories.Orders;
using HomeCycle.Infrastructure.Repositories.Payments;
using HomeCycle.Infrastructure.Repositories.Posts;
using HomeCycle.Infrastructure.Repositories.Products;
using HomeCycle.Infrastructure.Repositories.Profiles;
using HomeCycle.Infrastructure.Repositories.Reviews;
using HomeCycle.Infrastructure.Repositories.Shipments;
using HomeCycle.Infrastructure.Repositories.Users;
using HomeCycle.Infrastructure.Repositories.Wallets;
using HomeCycle.Infrastructure.Security;
using HomeCycle.Infrastructure.UnitOfWorks;
using MathNet.Numerics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

using HomeCycle.Application.Interfaces.Services.Appointments;
using HomeCycle.Application.Interfaces.Services.Orders;
using HomeCycle.Application.Services.Appointments;
using HomeCycle.Application.Services.Orders;
using HomeCycle.Infrastructure.Externals.PayOS;
using HomeCycle.Application.Interfaces.Repositories.GHN;
using HomeCycle.Infrastructure.Repositories.GHN;
using HomeCycle.Application.Interfaces.Services.GHN;
using HomeCycle.Application.Services.GHN;
using HomeCycle.Application.Interfaces.Services.Wallets;
using HomeCycle.Application.Services.Wallets;
using HomeCycle.Application.Interfaces.Repositories.Disputes;
using HomeCycle.Infrastructure.Repositories.Disputes;
using HomeCycle.Application.Interfaces.Services.Disputes;
using HomeCycle.Application.Services.Disputes;
using HomeCycle.Application.Interfaces.Repositories.PlatformPolicies;
using HomeCycle.Infrastructure.Repositories.PlatformPolicies;
using HomeCycle.Application.Interfaces.Services.PlatformPolicies;
using HomeCycle.Application.Services.PlatformPolicies;

namespace HomeCycle.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            //register DB
            services.AddDbContext<HomeCycleDbContext>(options =>
            {
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
            });

            //register UOW
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            //register hash password
            services.AddScoped<
                Microsoft.AspNetCore.Identity.IPasswordHasher<object>,
                Microsoft.AspNetCore.Identity.PasswordHasher<object>>();

            services.AddScoped<IPasswordHasher, PasswordHasherService>();

            //register GHN settings
            services.Configure<GhnSettings>(configuration.GetSection("GHNSettings"));

            //register JWT
            services.AddScoped<IJwtService, JwtService>();

            //register AutoMapper
            services.AddAutoMapper(cfg => cfg.AddMaps(typeof(MappingProfile).Assembly));

            // register FluentValidation
            // do nằm chung 1 application nên chỉ cần gọi 1 lần là đủ, không cần gọi nhiều lần
            services.AddValidatorsFromAssemblyContaining<RegisterPersonalRequestValidator>();
            services.AddValidatorsFromAssembly(typeof(LoginRequestValidator).Assembly);
            services.RemoveAll(typeof(IValidator<AgreementDetailsDto>));
            services.RemoveAll(typeof(AgreementDetailsDtoValidator));

            // register External Services
            services.AddScoped<IFileStorageService, FirebaseStorageService>();
            services.AddScoped<IOtpRepository, OtpRepository>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IBrandRepository, BrandRepository>();

            // register Repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IPersonalProfileRepository, PersonalProfileRepository>();
            services.AddScoped<IBusinessProfileRepository, BusinessProfileRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IBankAccountRepository, BankAccountRepository>();
            services.AddScoped<IProductAttributeOptionRepository, ProductAttributeOptionRepository>();
            services.AddScoped<IProductAttributeRepository, ProductAttributeRepository>();
            services.AddScoped<IProductTypeRepository, ProductTypeRepository>();
            services.AddScoped<IBrandRepository, BrandRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IPostRepository, PostRepository>();
            services.AddScoped<ICartItemRepository, CartRepository>();
            services.AddScoped<IMediaRepository, MediaRepository>();
            services.AddScoped<IProductAttributeValueRepository, ProductAttributeValueRepository>();
            services.AddScoped<IOfferRepository, OfferRepository>();
            services.AddScoped<INegotiationRepository, NegotiationRepository>();
            services.AddScoped<IMessageRepository, MessageRepository>();
            services.AddScoped<IBusinessProfileRepository, BusinessProfileRepository>();
            services.AddScoped<IBusinessDocumentRepository, BusinessDocumentRepository>();
            services.AddScoped<IBusinessProductTypeRepository, BusinessProductTypeRepository>();
            services.AddScoped<IBusinessServiceAreaRepository, BusinessServiceAreaRepository>();
            services.AddScoped<IBusinessProcurementPreferenceRepository, BusinessProcurementPreferenceRepository>();
            services.AddScoped<IAgreementFormRepository, AgreementFormRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();
            services.AddScoped<IWalletRepository, WalletRepository>();
            services.AddScoped<IWalletTransactionRepository, WalletTransactionRepository>();
            services.AddScoped<IWalletLedgerRepository, WalletLedgerRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IAppointmentRepository, AppointmentRepository>();
            services.AddScoped<ICollectionAppointmentRepository, CollectionAppointmentRepository>();
            services.AddScoped<IInspectionAppointmentRepository, InspectionAppointmentRepository>();
            services.AddScoped<IOfferRepository, OfferRepository>();
            services.AddScoped<IMessageRepository, MessageRepository>();
            services.AddScoped<INegotiationRepository, NegotiationRepository>();
            services.AddScoped<IMessageRepository, MessageRepository>();
            services.AddScoped<IGhnShipmentRepository, GhnShipmentRepository>();
            services.AddScoped<IShipmentRepository, ShipmentRepository>();
            services.AddScoped<IReviewRepository, ReviewRepository>();
            services.AddScoped<IGhnShipmentCreationService, GhnShipmentCreationService>();
            services.AddScoped<IGhnTrackingSyncService, GhnTrackingSyncService>();
            services.AddScoped<IGhnWebhookService, GhnWebhookService>();
            services.AddScoped<IWithdrawalRepository, WithdrawalRepository>();
            services.AddScoped<IDisputeRepository, DisputeRepository>();
            services.AddScoped<IPlatformPolicyRepository, PlatformPolicyRepository>();

            // register Services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IPersonalProfileService, PersonalProfileService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IBrandService, BrandService>();
            services.AddScoped<IProductTypeService, ProductTypeService>();
            services.AddScoped<IProductAttributeService, ProductAttributeService>();
            services.AddScoped<IPostService, PostService>();
            services.AddScoped<ICartService, CartService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IMediaService, MediaService>();
            services.AddScoped<IProductAttributeOptionService, ProductAttributeOptionService>();
            services.AddScoped<IOfferService, OfferService>();
            services.AddScoped<INegotiationService, NegotiationService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IBusinessProfileService, BusinessProfileService>();
            services.AddScoped<IModeratorService, ModeratorService>();
            services.AddScoped<IAgreementFormService, AgreementFormService>();
            services.AddScoped<IOfferService, OfferService>();
            services.AddScoped<IOfferTermsPolicy, OfferTermsPolicy>();
            services.AddScoped<INegotiationService, NegotiationService>();
            services.AddScoped<IMessageService, MessageService>();
            services.AddScoped<IAgreementFormService, AgreementFormService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<IAppointmentService, AppointmentService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IReviewService, ReviewService>();
            services.AddScoped<IWithdrawalService, WithdrawalService>();
            services.AddScoped<IWalletService, WalletService>();
            services.AddScoped<IDisputeService, DisputeService>();
            services.AddScoped<IDisputeWindowPolicy, DisputeWindowPolicy>();
            services.AddScoped<IDisputeTargetHandler, OrderDisputeTargetHandler>();
            services.AddScoped<PlatformPolicyService>();
            services.AddScoped<IPlatformPolicyService>(sp => sp.GetRequiredService<PlatformPolicyService>());
            services.AddScoped<IPlatformPolicyProvider>(sp => sp.GetRequiredService<PlatformPolicyService>());
            services.AddScoped<IAppointmentLifecycleJobService, AppointmentLifecycleJobService>();



            services.Configure<PayOSSettings>(configuration.GetSection("PayOS"));
            services.AddScoped<IPaymentGatewayService, PayOSGatewayAdapter>();
            services.Configure<PayOSPayoutSettings>(configuration.GetSection("PayOSPayout"));
            services.AddScoped<IPayoutGatewayService, PayOSPayoutAdapter>();

            services.AddOptions<GhnSettings>().Bind(configuration.GetSection(GhnSettings.SectionName))
                .Validate(
                    x => Uri.TryCreate(x.BaseUrl, UriKind.Absolute, out _), "GHN BaseUrl không hợp lệ.")
                .Validate(
                    x => x.BaseUrl.EndsWith("/", StringComparison.Ordinal), "GHN BaseUrl phải kết thúc bằng '/'.")
                .Validate(
                    x => !string.IsNullOrWhiteSpace(x.Token), "GHN Token chưa được cấu hình.")
                .Validate(
                    x => x.ShopId > 0, "GHN ShopId chưa được cấu hình.")
                .Validate(
                    x => x.TimeoutSeconds is >= 5 and <= 120, "GHN TimeoutSeconds phải từ 5 đến 120 giây.")
                .ValidateOnStart();

            services.AddMemoryCache();

            services.AddHttpClient<IGhnService, GhnService>(
                (serviceProvider, client) =>
                {
                    var settings = serviceProvider.GetRequiredService<IOptions<GhnSettings>>().Value;

                    // Kiểm tra tính hợp lệ của cấu hình trước khi chạy ứng dụng
                    ArgumentException.ThrowIfNullOrWhiteSpace(settings.BaseUrl);
                    ArgumentException.ThrowIfNullOrWhiteSpace(settings.Token);

                    client.BaseAddress = new Uri(settings.BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);

                    // Thiết lập các Header mặc định bắt buộc theo quy định của GHN API
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    client.DefaultRequestHeaders.TryAddWithoutValidation("Token", settings.Token);

                    // GHN Create Order yêu cầu cả Token và ShopId trong header
                    client.DefaultRequestHeaders.TryAddWithoutValidation("ShopId", settings.ShopId.ToString());

                    // Thiết lập timeout mặc định nếu chưa được cấu hình
                    client.Timeout = TimeSpan.FromSeconds(15);
                });

            return services;
        }
    }
}
