using Rebet.Application.Interfaces;
using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using MediatR;

namespace Rebet.Application.Commands.Expert;

public class SubscribeToExpertCommandHandler : IRequestHandler<SubscribeToExpertCommand, SubscribeToExpertResponse>
{
    private readonly IExpertRepository _expertRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IMediator _mediator;

    public SubscribeToExpertCommandHandler(
        IExpertRepository expertRepository,
        IUserRepository userRepository,
        ISubscriptionRepository subscriptionRepository,
        IMediator mediator)
    {
        _expertRepository = expertRepository;
        _userRepository = userRepository;
        _subscriptionRepository = subscriptionRepository;
        _mediator = mediator;
    }

    public async Task<SubscribeToExpertResponse> Handle(SubscribeToExpertCommand request, CancellationToken cancellationToken)
    {
        // Validate expert exists
        var expert = await _expertRepository.GetByIdAsync(request.ExpertId, cancellationToken);
        if (expert == null || expert.IsDeleted)
        {
            throw new KeyNotFoundException($"Expert with ID {request.ExpertId} not found");
        }

        // Validate user exists
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null || user.IsDeleted)
        {
            throw new KeyNotFoundException($"User with ID {request.UserId} not found");
        }

        // Check if user is trying to subscribe to themselves
        if (expert.UserId == request.UserId)
        {
            throw new InvalidOperationException("Cannot subscribe to your own expert profile");
        }

        // Get existing subscription
        var existingSubscription = await _subscriptionRepository.GetByUserAndExpertAsync(
            request.UserId,
            request.ExpertId,
            cancellationToken);

        bool isSubscribed;
        int subscriberCount;

        if (existingSubscription != null)
        {
            // Toggle subscription
            if (existingSubscription.Status == SubscriptionStatus.Active)
            {
                // Unsubscribe
                existingSubscription.Status = SubscriptionStatus.Cancelled;
                existingSubscription.UnsubscribedAt = DateTime.UtcNow;
                isSubscribed = false;
                
                // Decrement subscriber count
                expert.Statistics ??= new ExpertStatistics { ExpertId = expert.Id };
                if (expert.Statistics.TotalSubscribers > 0)
                {
                    expert.Statistics.TotalSubscribers--;
                }
                
                await _subscriptionRepository.UpdateAsync(existingSubscription, cancellationToken);
            }
            else
            {
                // Re-subscribe
                existingSubscription.Status = SubscriptionStatus.Active;
                existingSubscription.UnsubscribedAt = null;
                existingSubscription.SubscribedAt = DateTime.UtcNow;
                isSubscribed = true;
                
                // Increment subscriber count
                expert.Statistics ??= new ExpertStatistics { ExpertId = expert.Id };
                expert.Statistics.TotalSubscribers++;
                
                await _subscriptionRepository.UpdateAsync(existingSubscription, cancellationToken);
            }
        }
        else
        {
            // Create new subscription
            var subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                ExpertId = request.ExpertId,
                Status = SubscriptionStatus.Active,
                ReceiveNotifications = true,
                SubscribedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await _subscriptionRepository.AddAsync(subscription, cancellationToken);
            isSubscribed = true;
            
            // Increment subscriber count
            expert.Statistics ??= new ExpertStatistics { ExpertId = expert.Id };
            expert.Statistics.TotalSubscribers++;
        }

        await _expertRepository.SaveChangesAsync(cancellationToken);

        // Get updated subscriber count
        subscriberCount = expert.Statistics?.TotalSubscribers ?? 0;
        if (subscriberCount == 0)
        {
            // Fallback: count active subscriptions
            subscriberCount = await _subscriptionRepository.GetActiveSubscriberCountAsync(
                request.ExpertId,
                cancellationToken);
        }

        return new SubscribeToExpertResponse
        {
            ExpertId = request.ExpertId,
            IsSubscribed = isSubscribed,
            SubscriberCount = subscriberCount
        };
    }
}

