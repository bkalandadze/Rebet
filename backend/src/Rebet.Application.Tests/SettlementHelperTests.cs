using BettingPlatform.Application.Helpers;
using BettingPlatform.Domain.Entities;
using BettingPlatform.Domain.Enums;
using PositionEntity = BettingPlatform.Domain.Entities.Position;

namespace BettingPlatform.Application.Tests;

public class SettlementHelperTests
{
    #region DetermineMatchResult Tests

    [Fact]
    public void DetermineMatchResult_HomeWin_ShouldReturnWon()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Match Result",
            Selection = "Home"
        };

        var result = new EventResult
        {
            Winner = "Home",
            MarketResultsJson = "{\"matchResult\":\"home\",\"totalGoals\":3,\"homeScore\":2,\"awayScore\":1}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineMatchResult(position, result);

        // Assert
        Assert.Equal(PositionResult.Won, positionResult);
    }

    [Fact]
    public void DetermineMatchResult_AwayWin_ShouldReturnLost()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Match Result",
            Selection = "Home"
        };

        var result = new EventResult
        {
            Winner = "Away",
            MarketResultsJson = "{\"matchResult\":\"away\",\"totalGoals\":2,\"homeScore\":0,\"awayScore\":2}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineMatchResult(position, result);

        // Assert
        Assert.Equal(PositionResult.Lost, positionResult);
    }

    [Fact]
    public void DetermineMatchResult_Draw_ShouldReturnWon()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Match Result",
            Selection = "Draw"
        };

        var result = new EventResult
        {
            Winner = "Draw",
            MarketResultsJson = "{\"matchResult\":\"draw\",\"totalGoals\":2,\"homeScore\":1,\"awayScore\":1}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineMatchResult(position, result);

        // Assert
        Assert.Equal(PositionResult.Won, positionResult);
    }

    [Fact]
    public void DetermineMatchResult_CaseInsensitive_ShouldWork()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Match Result",
            Selection = "HOME"
        };

        var result = new EventResult
        {
            Winner = "home",
            MarketResultsJson = "{\"matchResult\":\"home\",\"totalGoals\":1,\"homeScore\":1,\"awayScore\":0}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineMatchResult(position, result);

        // Assert
        Assert.Equal(PositionResult.Won, positionResult);
    }

    [Fact]
    public void DetermineMatchResult_EventCancelled_ShouldReturnVoid()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Match Result",
            Selection = "Home"
        };

        var result = new EventResult
        {
            Winner = "Home",
            MarketResultsJson = "{\"cancelled\":true}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineMatchResult(position, result);

        // Assert
        Assert.Equal(PositionResult.Void, positionResult);
    }

    [Fact]
    public void DetermineMatchResult_EventAbandoned_ShouldReturnVoid()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Match Result",
            Selection = "Home"
        };

        var result = new EventResult
        {
            Winner = "Home",
            MarketResultsJson = "{\"abandoned\":true}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineMatchResult(position, result);

        // Assert
        Assert.Equal(PositionResult.Void, positionResult);
    }

    [Fact]
    public void DetermineMatchResult_NullResult_ShouldReturnVoid()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Match Result",
            Selection = "Home"
        };

        // Act
        var positionResult = SettlementHelper.DetermineMatchResult(position, null!);

        // Assert
        Assert.Equal(PositionResult.Void, positionResult);
    }

    [Fact]
    public void DetermineMatchResult_NullWinner_ShouldReturnVoid()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Match Result",
            Selection = "Home"
        };

        var result = new EventResult
        {
            Winner = null,
            MarketResultsJson = "{\"totalGoals\":2}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineMatchResult(position, result);

        // Assert
        Assert.Equal(PositionResult.Void, positionResult);
    }

    #endregion

    #region DetermineOverUnder Tests

    [Fact]
    public void DetermineOverUnder_OverWins_ShouldReturnWon()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Over/Under",
            Selection = "Over 2.5"
        };

        var result = new EventResult
        {
            MarketResultsJson = "{\"totalGoals\":3,\"homeScore\":2,\"awayScore\":1}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineOverUnder(position, result);

        // Assert
        Assert.Equal(PositionResult.Won, positionResult);
    }

    [Fact]
    public void DetermineOverUnder_OverLoses_ShouldReturnLost()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Over/Under",
            Selection = "Over 2.5"
        };

        var result = new EventResult
        {
            MarketResultsJson = "{\"totalGoals\":1,\"homeScore\":1,\"awayScore\":0}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineOverUnder(position, result);

        // Assert
        Assert.Equal(PositionResult.Lost, positionResult);
    }

    [Fact]
    public void DetermineOverUnder_UnderWins_ShouldReturnWon()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Over/Under",
            Selection = "Under 2.5"
        };

        var result = new EventResult
        {
            MarketResultsJson = "{\"totalGoals\":1,\"homeScore\":1,\"awayScore\":0}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineOverUnder(position, result);

        // Assert
        Assert.Equal(PositionResult.Won, positionResult);
    }

    [Fact]
    public void DetermineOverUnder_ExactMatchWholeNumber_ShouldReturnVoid()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Over/Under",
            Selection = "Over 2"
        };

        var result = new EventResult
        {
            MarketResultsJson = "{\"totalGoals\":2,\"homeScore\":1,\"awayScore\":1}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineOverUnder(position, result);

        // Assert
        // Exact match on whole number line is a push (void)
        Assert.Equal(PositionResult.Void, positionResult);
    }

    [Fact]
    public void DetermineOverUnder_CalculatesFromScores_ShouldWork()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Over/Under",
            Selection = "Over 2.5"
        };

        var result = new EventResult
        {
            MarketResultsJson = "{\"homeScore\":2,\"awayScore\":2}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineOverUnder(position, result);

        // Assert
        Assert.Equal(PositionResult.Won, positionResult); // 2+2 = 4 > 2.5
    }

    [Fact]
    public void DetermineOverUnder_InvalidSelection_ShouldReturnVoid()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Over/Under",
            Selection = "Invalid"
        };

        var result = new EventResult
        {
            MarketResultsJson = "{\"totalGoals\":3}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineOverUnder(position, result);

        // Assert
        Assert.Equal(PositionResult.Void, positionResult);
    }

    [Fact]
    public void DetermineOverUnder_EventCancelled_ShouldReturnVoid()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Over/Under",
            Selection = "Over 2.5"
        };

        var result = new EventResult
        {
            MarketResultsJson = "{\"cancelled\":true}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineOverUnder(position, result);

        // Assert
        Assert.Equal(PositionResult.Void, positionResult);
    }

    [Fact]
    public void DetermineOverUnder_NullResult_ShouldReturnVoid()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Over/Under",
            Selection = "Over 2.5"
        };

        // Act
        var positionResult = SettlementHelper.DetermineOverUnder(position, null!);

        // Assert
        Assert.Equal(PositionResult.Void, positionResult);
    }

    [Fact]
    public void DetermineOverUnder_NoTotalGoals_ShouldReturnVoid()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Over/Under",
            Selection = "Over 2.5"
        };

        var result = new EventResult
        {
            MarketResultsJson = "{}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineOverUnder(position, result);

        // Assert
        Assert.Equal(PositionResult.Void, positionResult);
    }

    #endregion

    #region DetermineBothTeamsScore Tests

    [Fact]
    public void DetermineBothTeamsScore_YesWins_ShouldReturnWon()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Both Teams Score",
            Selection = "Yes"
        };

        var result = new EventResult
        {
            MarketResultsJson = "{\"bothTeamsScore\":true,\"homeScore\":2,\"awayScore\":1}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineBothTeamsScore(position, result);

        // Assert
        Assert.Equal(PositionResult.Won, positionResult);
    }

    [Fact]
    public void DetermineBothTeamsScore_YesLoses_ShouldReturnLost()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Both Teams Score",
            Selection = "Yes"
        };

        var result = new EventResult
        {
            MarketResultsJson = "{\"bothTeamsScore\":false,\"homeScore\":2,\"awayScore\":0}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineBothTeamsScore(position, result);

        // Assert
        Assert.Equal(PositionResult.Lost, positionResult);
    }

    [Fact]
    public void DetermineBothTeamsScore_NoWins_ShouldReturnWon()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Both Teams Score",
            Selection = "No"
        };

        var result = new EventResult
        {
            MarketResultsJson = "{\"bothTeamsScore\":false,\"homeScore\":2,\"awayScore\":0}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineBothTeamsScore(position, result);

        // Assert
        Assert.Equal(PositionResult.Won, positionResult);
    }

    [Fact]
    public void DetermineBothTeamsScore_CalculatesFromScores_ShouldWork()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Both Teams Score",
            Selection = "Yes"
        };

        var result = new EventResult
        {
            MarketResultsJson = "{\"homeScore\":2,\"awayScore\":1}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineBothTeamsScore(position, result);

        // Assert
        Assert.Equal(PositionResult.Won, positionResult);
    }

    [Fact]
    public void DetermineBothTeamsScore_CalculatesFromScores_No_ShouldWork()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Both Teams Score",
            Selection = "No"
        };

        var result = new EventResult
        {
            MarketResultsJson = "{\"homeScore\":2,\"awayScore\":0}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineBothTeamsScore(position, result);

        // Assert
        Assert.Equal(PositionResult.Won, positionResult);
    }

    [Fact]
    public void DetermineBothTeamsScore_CaseInsensitive_ShouldWork()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Both Teams Score",
            Selection = "YES"
        };

        var result = new EventResult
        {
            MarketResultsJson = "{\"bothTeamsScore\":true}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineBothTeamsScore(position, result);

        // Assert
        Assert.Equal(PositionResult.Won, positionResult);
    }

    [Fact]
    public void DetermineBothTeamsScore_InvalidSelection_ShouldReturnVoid()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Both Teams Score",
            Selection = "Maybe"
        };

        var result = new EventResult
        {
            MarketResultsJson = "{\"bothTeamsScore\":true}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineBothTeamsScore(position, result);

        // Assert
        Assert.Equal(PositionResult.Void, positionResult);
    }

    [Fact]
    public void DetermineBothTeamsScore_EventCancelled_ShouldReturnVoid()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Both Teams Score",
            Selection = "Yes"
        };

        var result = new EventResult
        {
            MarketResultsJson = "{\"cancelled\":true}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineBothTeamsScore(position, result);

        // Assert
        Assert.Equal(PositionResult.Void, positionResult);
    }

    [Fact]
    public void DetermineBothTeamsScore_NullResult_ShouldReturnVoid()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Both Teams Score",
            Selection = "Yes"
        };

        // Act
        var positionResult = SettlementHelper.DetermineBothTeamsScore(position, null!);

        // Assert
        Assert.Equal(PositionResult.Void, positionResult);
    }

    #endregion

    #region DetermineAsianHandicap Tests

    [Fact]
    public void DetermineAsianHandicap_HomeWins_ShouldReturnWon()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Asian Handicap",
            Selection = "Home -1.5"
        };

        var result = new EventResult
        {
            MarketResultsJson = "{\"homeScore\":3,\"awayScore\":1}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineAsianHandicap(position, result);

        // Assert
        // Home: 3 - 1.5 = 1.5, Away: 1, Home wins
        Assert.Equal(PositionResult.Won, positionResult);
    }

    [Fact]
    public void DetermineAsianHandicap_HomeLoses_ShouldReturnLost()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Asian Handicap",
            Selection = "Home -1.5"
        };

        var result = new EventResult
        {
            MarketResultsJson = "{\"homeScore\":2,\"awayScore\":1}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineAsianHandicap(position, result);

        // Assert
        // Home: 2 - 1.5 = 0.5, Away: 1, Away wins
        Assert.Equal(PositionResult.Lost, positionResult);
    }

    [Fact]
    public void DetermineAsianHandicap_AwayWins_ShouldReturnWon()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Asian Handicap",
            Selection = "Away +0.5"
        };

        var result = new EventResult
        {
            MarketResultsJson = "{\"homeScore\":1,\"awayScore\":1}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineAsianHandicap(position, result);

        // Assert
        // Home: 1, Away: 1 + 0.5 = 1.5, Away wins
        Assert.Equal(PositionResult.Won, positionResult);
    }

    [Fact]
    public void DetermineAsianHandicap_Draw_ShouldReturnVoid()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Asian Handicap",
            Selection = "Home -1.5"
        };

        var result = new EventResult
        {
            MarketResultsJson = "{\"homeScore\":2,\"awayScore\":0}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineAsianHandicap(position, result);

        // Assert
        // Home: 2 - 1.5 = 0.5, Away: 0, Home wins (not a draw)
        // Let's test actual draw scenario
        var position2 = new PositionEntity
        {
            Market = "Asian Handicap",
            Selection = "Home -1"
        };

        var result2 = new EventResult
        {
            MarketResultsJson = "{\"homeScore\":2,\"awayScore\":1}"
        };

        var positionResult2 = SettlementHelper.DetermineAsianHandicap(position2, result2);

        // Home: 2 - 1 = 1, Away: 1, Draw
        Assert.Equal(PositionResult.Void, positionResult2);
    }

    [Fact]
    public void DetermineAsianHandicap_ParsesFromFinalScore_ShouldWork()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Asian Handicap",
            Selection = "Home -1.5"
        };

        var result = new EventResult
        {
            FinalScore = "3-1"
        };

        // Act
        var positionResult = SettlementHelper.DetermineAsianHandicap(position, result);

        // Assert
        // Home: 3 - 1.5 = 1.5, Away: 1, Home wins
        Assert.Equal(PositionResult.Won, positionResult);
    }

    [Fact]
    public void DetermineAsianHandicap_InvalidSelection_ShouldReturnVoid()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Asian Handicap",
            Selection = "Invalid"
        };

        var result = new EventResult
        {
            MarketResultsJson = "{\"homeScore\":2,\"awayScore\":1}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineAsianHandicap(position, result);

        // Assert
        Assert.Equal(PositionResult.Void, positionResult);
    }

    [Fact]
    public void DetermineAsianHandicap_EventCancelled_ShouldReturnVoid()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Asian Handicap",
            Selection = "Home -1.5"
        };

        var result = new EventResult
        {
            MarketResultsJson = "{\"cancelled\":true}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineAsianHandicap(position, result);

        // Assert
        Assert.Equal(PositionResult.Void, positionResult);
    }

    [Fact]
    public void DetermineAsianHandicap_NullResult_ShouldReturnVoid()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Asian Handicap",
            Selection = "Home -1.5"
        };

        // Act
        var positionResult = SettlementHelper.DetermineAsianHandicap(position, null!);

        // Assert
        Assert.Equal(PositionResult.Void, positionResult);
    }

    [Fact]
    public void DetermineAsianHandicap_NoScores_ShouldReturnVoid()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Asian Handicap",
            Selection = "Home -1.5"
        };

        var result = new EventResult
        {
            MarketResultsJson = "{}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineAsianHandicap(position, result);

        // Assert
        Assert.Equal(PositionResult.Void, positionResult);
    }

    [Fact]
    public void DetermineAsianHandicap_PositiveHandicap_ShouldWork()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Asian Handicap",
            Selection = "Away +1.5"
        };

        var result = new EventResult
        {
            MarketResultsJson = "{\"homeScore\":2,\"awayScore\":0}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineAsianHandicap(position, result);

        // Assert
        // Home: 2, Away: 0 + 1.5 = 1.5, Home still wins
        Assert.Equal(PositionResult.Lost, positionResult);
    }

    #endregion

    #region Requested Tests (Specific Names)

    [Fact]
    public void Test_MatchResult_HomeWin_ReturnsWon()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Match Result",
            Selection = "Home"
        };

        var result = new EventResult
        {
            Winner = "Home",
            MarketResultsJson = "{\"matchResult\":\"home\",\"totalGoals\":2,\"homeScore\":2,\"awayScore\":0}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineMatchResult(position, result);

        // Assert
        Assert.Equal(PositionResult.Won, positionResult);
    }

    [Fact]
    public void Test_MatchResult_HomeLoss_ReturnsLost()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Match Result",
            Selection = "Home"
        };

        var result = new EventResult
        {
            Winner = "Away",
            MarketResultsJson = "{\"matchResult\":\"away\",\"totalGoals\":1,\"homeScore\":0,\"awayScore\":1}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineMatchResult(position, result);

        // Assert
        Assert.Equal(PositionResult.Lost, positionResult);
    }

    [Fact]
    public void Test_OverUnder_Over25_ThreeGoals_ReturnsWon()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Over/Under",
            Selection = "Over 2.5"
        };

        var result = new EventResult
        {
            MarketResultsJson = "{\"totalGoals\":3,\"homeScore\":2,\"awayScore\":1}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineOverUnder(position, result);

        // Assert
        Assert.Equal(PositionResult.Won, positionResult);
    }

    [Fact]
    public void Test_OverUnder_Under25_TwoGoals_ReturnsWon()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Over/Under",
            Selection = "Under 2.5"
        };

        var result = new EventResult
        {
            MarketResultsJson = "{\"totalGoals\":2,\"homeScore\":1,\"awayScore\":1}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineOverUnder(position, result);

        // Assert
        // 2 goals < 2.5, so Under wins
        Assert.Equal(PositionResult.Won, positionResult);
    }

    [Fact]
    public void Test_BothTeamsScore_Yes_BothScored_ReturnsWon()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Both Teams Score",
            Selection = "Yes"
        };

        var result = new EventResult
        {
            MarketResultsJson = "{\"bothTeamsScore\":true,\"homeScore\":2,\"awayScore\":1}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineBothTeamsScore(position, result);

        // Assert
        Assert.Equal(PositionResult.Won, positionResult);
    }

    [Fact]
    public void Test_AsianHandicap_Various_Scenarios()
    {
        // Scenario 1: Home -1.5, Home wins by 2+ goals
        var position1 = new PositionEntity
        {
            Market = "Asian Handicap",
            Selection = "Home -1.5"
        };

        var result1 = new EventResult
        {
            MarketResultsJson = "{\"homeScore\":3,\"awayScore\":0}"
        };

        var result1_actual = SettlementHelper.DetermineAsianHandicap(position1, result1);
        Assert.Equal(PositionResult.Won, result1_actual); // Home: 3-1.5=1.5, Away: 0, Home wins

        // Scenario 2: Home -1.5, Home wins by exactly 1 goal (loses)
        var result2 = new EventResult
        {
            MarketResultsJson = "{\"homeScore\":2,\"awayScore\":1}"
        };

        var result2_actual = SettlementHelper.DetermineAsianHandicap(position1, result2);
        Assert.Equal(PositionResult.Lost, result2_actual); // Home: 2-1.5=0.5, Away: 1, Away wins

        // Scenario 3: Away +0.5, Draw becomes win
        var position3 = new PositionEntity
        {
            Market = "Asian Handicap",
            Selection = "Away +0.5"
        };

        var result3 = new EventResult
        {
            MarketResultsJson = "{\"homeScore\":1,\"awayScore\":1}"
        };

        var result3_actual = SettlementHelper.DetermineAsianHandicap(position3, result3);
        Assert.Equal(PositionResult.Won, result3_actual); // Home: 1, Away: 1+0.5=1.5, Away wins

        // Scenario 4: Home -0.5, Exact draw becomes void
        var position4 = new PositionEntity
        {
            Market = "Asian Handicap",
            Selection = "Home -1"
        };

        var result4 = new EventResult
        {
            MarketResultsJson = "{\"homeScore\":2,\"awayScore\":1}"
        };

        var result4_actual = SettlementHelper.DetermineAsianHandicap(position4, result4);
        Assert.Equal(PositionResult.Void, result4_actual); // Home: 2-1=1, Away: 1, Draw (void)

        // Scenario 5: Away +1.5, Away loses by 1 goal but wins with handicap
        var position5 = new PositionEntity
        {
            Market = "Asian Handicap",
            Selection = "Away +1.5"
        };

        var result5 = new EventResult
        {
            MarketResultsJson = "{\"homeScore\":2,\"awayScore\":0}"
        };

        var result5_actual = SettlementHelper.DetermineAsianHandicap(position5, result5);
        Assert.Equal(PositionResult.Lost, result5_actual); // Home: 2, Away: 0+1.5=1.5, Home still wins

        // Scenario 6: Home +0.5, Home draws but wins with handicap
        var position6 = new PositionEntity
        {
            Market = "Asian Handicap",
            Selection = "Home +0.5"
        };

        var result6 = new EventResult
        {
            MarketResultsJson = "{\"homeScore\":1,\"awayScore\":1}"
        };

        var result6_actual = SettlementHelper.DetermineAsianHandicap(position6, result6);
        Assert.Equal(PositionResult.Won, result6_actual); // Home: 1+0.5=1.5, Away: 1, Home wins
    }

    #endregion

    #region Additional Edge Case Tests

    [Fact]
    public void Test_MatchResult_DrawSelection_ReturnsWon_WhenDraw()
    {
        // Arrange
        var position = new PositionEntity
        {
            Market = "Match Result",
            Selection = "Draw"
        };

        var result = new EventResult
        {
            Winner = "Draw",
            MarketResultsJson = "{\"matchResult\":\"draw\",\"totalGoals\":0,\"homeScore\":0,\"awayScore\":0}"
        };

        // Act
        var positionResult = SettlementHelper.DetermineMatchResult(position, result);

        // Assert
        Assert.Equal(PositionResult.Won, positionResult);
    }

    [Fact]
    public void Test_MatchResult_AlternativeSelectionFormats_ShouldWork()
    {
        // Test "Home Win" format
        var position1 = new PositionEntity
        {
            Market = "Match Result",
            Selection = "Home Win"
        };

        var result1 = new EventResult
        {
            Winner = "Home",
            MarketResultsJson = "{\"matchResult\":\"home\"}"
        };

        Assert.Equal(PositionResult.Won, SettlementHelper.DetermineMatchResult(position1, result1));

        // Test "1" format (Home)
        var position2 = new PositionEntity
        {
            Market = "Match Result",
            Selection = "1"
        };

        Assert.Equal(PositionResult.Won, SettlementHelper.DetermineMatchResult(position2, result1));

        // Test "2" format (Away)
        var position3 = new PositionEntity
        {
            Market = "Match Result",
            Selection = "2"
        };

        var result3 = new EventResult
        {
            Winner = "Away",
            MarketResultsJson = "{\"matchResult\":\"away\"}"
        };

        Assert.Equal(PositionResult.Won, SettlementHelper.DetermineMatchResult(position3, result3));

        // Test "X" format (Draw)
        var position4 = new PositionEntity
        {
            Market = "Match Result",
            Selection = "X"
        };

        var result4 = new EventResult
        {
            Winner = "Draw",
            MarketResultsJson = "{\"matchResult\":\"draw\"}"
        };

        Assert.Equal(PositionResult.Won, SettlementHelper.DetermineMatchResult(position4, result4));
    }

    [Fact]
    public void Test_OverUnder_ExactMatchHalfLine_ShouldReturnLost()
    {
        // For half lines (e.g., 2.5), exact match is impossible since totalGoals is integer
        // But if we had 2.5 goals exactly, it would be a push - but since it's integer, this tests edge case
        var position = new PositionEntity
        {
            Market = "Over/Under",
            Selection = "Over 2.5"
        };

        var result = new EventResult
        {
            MarketResultsJson = "{\"totalGoals\":2,\"homeScore\":1,\"awayScore\":1}"
        };

        // 2 < 2.5, so Over loses
        Assert.Equal(PositionResult.Lost, SettlementHelper.DetermineOverUnder(position, result));
    }

    [Fact]
    public void Test_OverUnder_WholeNumberLine_ExactMatch_ShouldReturnVoid()
    {
        // Whole number line with exact match should be void (push)
        var position = new PositionEntity
        {
            Market = "Over/Under",
            Selection = "Over 3"
        };

        var result = new EventResult
        {
            MarketResultsJson = "{\"totalGoals\":3,\"homeScore\":2,\"awayScore\":1}"
        };

        Assert.Equal(PositionResult.Void, SettlementHelper.DetermineOverUnder(position, result));
    }

    [Fact]
    public void Test_OverUnder_CalculatesFromFinalScore_ShouldWork()
    {
        var position = new PositionEntity
        {
            Market = "Over/Under",
            Selection = "Over 2.5"
        };

        var result = new EventResult
        {
            FinalScore = "3-1"
        };

        // 3+1 = 4 > 2.5, should win
        Assert.Equal(PositionResult.Won, SettlementHelper.DetermineOverUnder(position, result));
    }

    [Fact]
    public void Test_BothTeamsScore_No_BothScored_ReturnsLost()
    {
        var position = new PositionEntity
        {
            Market = "Both Teams Score",
            Selection = "No"
        };

        var result = new EventResult
        {
            MarketResultsJson = "{\"bothTeamsScore\":true,\"homeScore\":2,\"awayScore\":1}"
        };

        Assert.Equal(PositionResult.Lost, SettlementHelper.DetermineBothTeamsScore(position, result));
    }

    [Fact]
    public void Test_BothTeamsScore_AlternativeFormats_ShouldWork()
    {
        // Test "True" format
        var position1 = new PositionEntity
        {
            Market = "Both Teams Score",
            Selection = "True"
        };

        var result1 = new EventResult
        {
            MarketResultsJson = "{\"bothTeamsScore\":true}"
        };

        Assert.Equal(PositionResult.Won, SettlementHelper.DetermineBothTeamsScore(position1, result1));

        // Test "1" format
        var position2 = new PositionEntity
        {
            Market = "Both Teams Score",
            Selection = "1"
        };

        Assert.Equal(PositionResult.Won, SettlementHelper.DetermineBothTeamsScore(position2, result1));

        // Test "False" format
        var position3 = new PositionEntity
        {
            Market = "Both Teams Score",
            Selection = "False"
        };

        var result3 = new EventResult
        {
            MarketResultsJson = "{\"bothTeamsScore\":false}"
        };

        Assert.Equal(PositionResult.Won, SettlementHelper.DetermineBothTeamsScore(position3, result3));

        // Test "0" format
        var position4 = new PositionEntity
        {
            Market = "Both Teams Score",
            Selection = "0"
        };

        Assert.Equal(PositionResult.Won, SettlementHelper.DetermineBothTeamsScore(position4, result3));
    }

    [Fact]
    public void Test_BothTeamsScore_CalculatesFromFinalScore_ShouldWork()
    {
        var position = new PositionEntity
        {
            Market = "Both Teams Score",
            Selection = "Yes"
        };

        var result = new EventResult
        {
            FinalScore = "2-1"
        };

        // Both teams scored (2 and 1), should win
        Assert.Equal(PositionResult.Won, SettlementHelper.DetermineBothTeamsScore(position, result));
    }

    [Fact]
    public void Test_BothTeamsScore_OneTeamZero_ReturnsLost_ForYes()
    {
        var position = new PositionEntity
        {
            Market = "Both Teams Score",
            Selection = "Yes"
        };

        var result = new EventResult
        {
            FinalScore = "2-0"
        };

        // Only home team scored, should lose
        Assert.Equal(PositionResult.Lost, SettlementHelper.DetermineBothTeamsScore(position, result));
    }

    [Fact]
    public void Test_AsianHandicap_NegativeHandicap_HomeWins_ShouldWork()
    {
        var position = new PositionEntity
        {
            Market = "Asian Handicap",
            Selection = "Home -0.5"
        };

        var result = new EventResult
        {
            MarketResultsJson = "{\"homeScore\":1,\"awayScore\":0}"
        };

        // Home: 1-0.5=0.5, Away: 0, Home wins
        Assert.Equal(PositionResult.Won, SettlementHelper.DetermineAsianHandicap(position, result));
    }

    [Fact]
    public void Test_AsianHandicap_PositiveHandicap_AwayWins_ShouldWork()
    {
        var position = new PositionEntity
        {
            Market = "Asian Handicap",
            Selection = "Away +2.5"
        };

        var result = new EventResult
        {
            MarketResultsJson = "{\"homeScore\":2,\"awayScore\":0}"
        };

        // Home: 2, Away: 0+2.5=2.5, Away wins
        Assert.Equal(PositionResult.Won, SettlementHelper.DetermineAsianHandicap(position, result));
    }

    [Fact]
    public void Test_AsianHandicap_WholeNumberHandicap_Draw_ShouldReturnVoid()
    {
        var position = new PositionEntity
        {
            Market = "Asian Handicap",
            Selection = "Home -2"
        };

        var result = new EventResult
        {
            MarketResultsJson = "{\"homeScore\":3,\"awayScore\":1}"
        };

        // Home: 3-2=1, Away: 1, Draw (void)
        Assert.Equal(PositionResult.Void, SettlementHelper.DetermineAsianHandicap(position, result));
    }

    [Fact]
    public void Test_AsianHandicap_ParsesFromFinalScore_WithColon_ShouldWork()
    {
        var position = new PositionEntity
        {
            Market = "Asian Handicap",
            Selection = "Home -1.5"
        };

        var result = new EventResult
        {
            FinalScore = "3:1"
        };

        // Home: 3-1.5=1.5, Away: 1, Home wins
        Assert.Equal(PositionResult.Won, SettlementHelper.DetermineAsianHandicap(position, result));
    }

    [Fact]
    public void Test_AllMarkets_EventAbandoned_ShouldReturnVoid()
    {
        var abandonedResult = new EventResult
        {
            MarketResultsJson = "{\"abandoned\":true}"
        };

        var position1 = new PositionEntity { Market = "Match Result", Selection = "Home" };
        var position2 = new PositionEntity { Market = "Over/Under", Selection = "Over 2.5" };
        var position3 = new PositionEntity { Market = "Both Teams Score", Selection = "Yes" };
        var position4 = new PositionEntity { Market = "Asian Handicap", Selection = "Home -1.5" };

        Assert.Equal(PositionResult.Void, SettlementHelper.DetermineMatchResult(position1, abandonedResult));
        Assert.Equal(PositionResult.Void, SettlementHelper.DetermineOverUnder(position2, abandonedResult));
        Assert.Equal(PositionResult.Void, SettlementHelper.DetermineBothTeamsScore(position3, abandonedResult));
        Assert.Equal(PositionResult.Void, SettlementHelper.DetermineAsianHandicap(position4, abandonedResult));
    }

    [Fact]
    public void Test_AllMarkets_NullResult_ShouldReturnVoid()
    {
        var position1 = new PositionEntity { Market = "Match Result", Selection = "Home" };
        var position2 = new PositionEntity { Market = "Over/Under", Selection = "Over 2.5" };
        var position3 = new PositionEntity { Market = "Both Teams Score", Selection = "Yes" };
        var position4 = new PositionEntity { Market = "Asian Handicap", Selection = "Home -1.5" };

        Assert.Equal(PositionResult.Void, SettlementHelper.DetermineMatchResult(position1, null!));
        Assert.Equal(PositionResult.Void, SettlementHelper.DetermineOverUnder(position2, null!));
        Assert.Equal(PositionResult.Void, SettlementHelper.DetermineBothTeamsScore(position3, null!));
        Assert.Equal(PositionResult.Void, SettlementHelper.DetermineAsianHandicap(position4, null!));
    }

    [Fact]
    public void Test_OverUnder_EdgeCases_ShouldHandleCorrectly()
    {
        // Test Over 0.5 with 1 goal
        var position1 = new PositionEntity
        {
            Market = "Over/Under",
            Selection = "Over 0.5"
        };

        var result1 = new EventResult
        {
            MarketResultsJson = "{\"totalGoals\":1}"
        };

        Assert.Equal(PositionResult.Won, SettlementHelper.DetermineOverUnder(position1, result1));

        // Test Under 0.5 with 0 goals
        var position2 = new PositionEntity
        {
            Market = "Over/Under",
            Selection = "Under 0.5"
        };

        var result2 = new EventResult
        {
            MarketResultsJson = "{\"totalGoals\":0}"
        };

        Assert.Equal(PositionResult.Won, SettlementHelper.DetermineOverUnder(position2, result2));

        // Test Over 10.5 with 11 goals
        var position3 = new PositionEntity
        {
            Market = "Over/Under",
            Selection = "Over 10.5"
        };

        var result3 = new EventResult
        {
            MarketResultsJson = "{\"totalGoals\":11}"
        };

        Assert.Equal(PositionResult.Won, SettlementHelper.DetermineOverUnder(position3, result3));
    }

    [Fact]
    public void Test_AsianHandicap_EdgeCases_ShouldHandleCorrectly()
    {
        // Test very large handicap
        var position1 = new PositionEntity
        {
            Market = "Asian Handicap",
            Selection = "Home -5.5"
        };

        var result1 = new EventResult
        {
            MarketResultsJson = "{\"homeScore\":6,\"awayScore\":0}"
        };

        Assert.Equal(PositionResult.Won, SettlementHelper.DetermineAsianHandicap(position1, result1));

        // Test very large positive handicap
        var position2 = new PositionEntity
        {
            Market = "Asian Handicap",
            Selection = "Away +5.5"
        };

        var result2 = new EventResult
        {
            MarketResultsJson = "{\"homeScore\":5,\"awayScore\":0}"
        };

        Assert.Equal(PositionResult.Won, SettlementHelper.DetermineAsianHandicap(position2, result2));

        // Test zero handicap (should be void on draw)
        var position3 = new PositionEntity
        {
            Market = "Asian Handicap",
            Selection = "Home 0"
        };

        var result3 = new EventResult
        {
            MarketResultsJson = "{\"homeScore\":1,\"awayScore\":1}"
        };

        // This should parse as invalid since "Home 0" doesn't match the pattern, but let's test
        var result3_actual = SettlementHelper.DetermineAsianHandicap(position3, result3);
        // Should be void due to invalid selection format
        Assert.Equal(PositionResult.Void, result3_actual);
    }

    #endregion
}

