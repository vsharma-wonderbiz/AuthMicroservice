    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using AuthMicroservice.Application.Dtos;
    using AuthMicroservice.Domain.Entities;
    using AutoMapper;

    namespace AuthMicroservice.Application.Mapping
    {
        public class MappingProfile : Profile
        {
            public MappingProfile()
            {
                // Map Domain Entity to DTO (core fix for List<User> -> List<UserDto>)
                CreateMap<User, UserDto>()
                    .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                    .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username))
                    .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                    .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role));

                // Input DTOs to Entity (for creation/updates)
                CreateMap<CreateUserDto, User>()
                    .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()); // Handled in service (hashing)

                CreateMap<UpdateUserDto, User>()
                    .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()); // Optional hashing in service

                // OAuth mappings (if used)
                CreateMap<OAuthUser, OAuthUserDto>();
                CreateMap<OAuthUser, UserDto>()
                    .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.OAuthUserId));
            }
        }
    }
