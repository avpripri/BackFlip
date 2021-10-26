Feature: Approach
  Display approach assist

  Scenario: Show approach assistant
    Given Approach assistant
    When I fly within 10nm of any airport
        And I'm aligned with an runway of that airport
        And the runway is within +/- 20 of yaw
    Then The approach assistant for that runway should display

  Scenario: Above glideslope
    Given Approach assistant
    When I fly +3 degrees glideslope
    Then I should see indications of +3 degress

  Scenario: Below glideslope
    Given Approach assistant
    When I fly -3 degrees glideslope
    Then I should see indications of -3 degress

  Scenario: Right of centerline
    Given Approach assistant
    When I fly +3 degrees of centerline
    Then I should see indications of +3 degress

  Scenario: Left of centerline
    Given Approach assistant
    When I fly -3 degrees of centerline
    Then I should see indications of -3 degress

  Scenario: Go Around
    Given Approach assistant
    When I fly "<OffSlopeConditions>"
    Then I should see a go around indicator

    Examples: OffSlopeConditions
      | TooHigh     |
      | TooLow      |
      | TooLeft     |
      | TooRight    |
      | TooFast     |
      | TooSlow     |