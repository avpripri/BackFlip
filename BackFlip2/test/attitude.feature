Feature: Attitude
  The pitch, roll, yaw and aoa of the aircraft should be displayed

  Scenario: pitch up
    Given pitch input is +10    
    When I display the attitude
    Then I should see the pitch display as +10

  Scenario: pitch down
    Given pitch input is -10    
    When I display the attitude
    Then I should see the pitch display as -10

  Scenario: pitch out of bounds
    Given pitch input is less than -90 OR greater than 90    
    When I display the attitude
    Then I should see pitch error

  Scenario: pitch missing
    Given pitch input is missing
    When I display the attitude
    Then I should see pitch error

  Scenario: aoa up
    Given aoa input is +10    
    When I display the attitude
    Then I should see the aoa display as +10

  Scenario: aoa down
    Given aoa input is -10    
    When I display the attitude
    Then I should see the aoa display as -10

  Scenario: aoa out of bounds
    Given aoa input is less than -90 OR greater than 90    
    When I display the attitude
    Then I should see aoa error

  Scenario: aoa missing
    Given aoa input is missing
    When I display the attitude
    Then I should see aoa error

  Scenario: roll right
    Given roll input is +10    
    When I display the attitude
    Then I should see the roll display as +10

  Scenario: roll left
    Given roll input is -10    
    When I display the attitude
    Then I should see the roll display as -10

  Scenario: roll out of bounds
    Given roll input is less than -180 OR greater than 180    
    When I display the attitude
    Then I should see roll error

  Scenario: roll missing
    Given roll input is missing
    When I display the attitude
    Then I should see roll error

  Scenario: yaw right
    Given yaw input is +10    
    When I display the attitude
    Then I should see the yaw display as +10

  Scenario: yaw left
    Given yaw input is -10    
    When I display the attitude
    Then I should see the yaw display as -10

  Scenario: yaw out of bounds
    Given yaw input is less than -90 OR greater than 90    
    When I display the attitude
    Then I should see yaw error

  Scenario: yaw missing
    Given yaw input is missing
    When I display the attitude
    Then I should see yaw error

