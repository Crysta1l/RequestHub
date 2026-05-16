using AutoMapper;
using RequestHub.DTOs;
using RequestHub.Models;

namespace RequestHub.Profiles
{
    public class MappingProfile : Profile 
    {
        public MappingProfile()
        {
            CreateMap<CreateRequestDto, AccessRequest>();
        }
    }
}
