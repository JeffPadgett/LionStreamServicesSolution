using AutoMapper;
using StreamServices.Core;
using StreamServices.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace StreamServices
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<Subscription, SubscriptionDTO>()
                .ForMember(d => d.BroadcasterUserId, opt => opt.MapFrom(src => src.Condition.BroadcasterUserId));
        }
    }
}
