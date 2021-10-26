Feature: Comms
  Display current and suggested communicaitons

  Scenario: Comms set to freq
    Given Comms display 
    When I set frequency to 123.45
    Then I should see primary frequnecy set to 123.45

  Scenario: Comms startup suggest ATIS
    Given Comms display 
    When I recently started display system
        And there's an ATIS frequency
    Then I should see ATIS frequency suggestion

  Scenario: Comms startup suggest CTAF
    Given Comms display 
    When I recently started display system
        And there's no ATIS frequency
        AND there's a CTAF frequency
    Then I should see CTAF frequency suggestion

  Scenario: Comms weather selected set barometer
    Given Comms display 
    When I select ATIS frequency
        And I recently started display system
        And haven't recently set the barometric pressure
    Then Set barometeric pressure for airfield altitude
        And Open edit for barometric pressure

  Scenario: Comms weather selected suggest clearance
    Given Comms display 
    When I select ATIS frequency
        And there's a clearance frequency
    Then I should see a clearance frequency suggestion

  Scenario: Comms weather selected suggest ground
    Given Comms display 
    When I select ATIS frequency
        And there's no clearance frequency
        And there's a ground frequency
    Then I should see a ground frequency suggestion

  Scenario: Comms weather selected suggest CTAF
    Given Comms display 
    When I select ATIS frequency
        And there's no clearance frequency
        And there's no ground frequency
        And there's a CTAF frequency
    Then I should see a CTAF frequency suggestion

  Scenario: Comms clearance selected suggest ground
    Given Comms display 
    When I select clearance frequency
        And there's a ground frequency
    Then I should see a ground frequency suggestion

  Scenario: Comms ground selected suggest tower
    Given Comms display 
    When I select ground frequency
        And there's a tower frequency
    Then I should see a tower frequency suggestion

  Scenario: Comms tower selected suggest departure
    Given Comms display 
    When I select tower frequency
        And there's a departure frequency
    Then I should see a departure frequency suggestion

  Scenario: Comms CTAF selected suggest departure
    Given Comms display 
    When I select CTAF frequency
        And there's a departure frequency
    Then I should see a departure frequency suggestion

  Scenario: Comms enroute frequency suggestions
    Given Comms display 
    When I fly into enroute airpspace
    Then I should see enroute frequency suggestions

  Scenario: Comms enroute selected suggest tower
    Given Comms display 
    When I have an enroute frequency selected
        And I fly within 20nm of my destination airport
        And there's a tower frequency
    Then I should see a tower frequency suggestion

  Scenario: Comms enroute selected suggest CTAF
    Given Comms display 
    When I have an enroute frequency selected
        And I fly within 20nm of my destination airport
        And there's a CTAF frequency
    Then I should see a CTAF frequency suggestion

  Scenario: Comms tower to ground suggestion
    Given Comms display 
    When I select tower frequency
        And I landed
        And there's a ground frequency
    Then I should see a ground frequency suggestion

  Scenario: Comms missing
    Given Comms display is missing 
    When I display the comms panel
    Then I should see comms error