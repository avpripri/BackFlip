Feature: Planning
  Intelligent flight planning

  Scenario: Load filed flight plan
    Given The flight planner
    When I recently have started up the system
        And there's a flight plan on file
        And location from is near current location
    Then Load flight plan
        And Request Activate flight plan

  Scenario: Defining a flight plan
    Given The flight planner
    When I recently have started up the system
        And there's no flight plan on file
    Then Request destination from pilot

  Scenario: IFR Flight Rules
    Given The flight planner
    When A destination is loaded
        And I have the weather
        And weather is below VFR minimums
        And Is capable of being performed under IFR rules with equipement
    Then Generate IFR flight plan
        And Request to file IFR flight plan

  Scenario: VFR Flight Plan
    Given The flight planner
    When A destination is loaded
        And I have the weather
        And weather for the entire flight is above VFR minimums
    Then Generate VFR flight plan
        And Load VFR flight plan

  Scenario: Request to file VFR flight plan
    Given The flight planner
    When VFR flight plan is loaded
        And Destination is over 50nm
    Then Request to file VFR flight plan

  Scenario: VFR arrival
    Given The flight planner
    When VFR flight plan is loaded
    Then Determine best arrival at destination

  Scenario: IFR arrival
    Given The flight planner
    When IFR flight plan is loaded
    Then Determine best arrivals at destination
        And Allow pilot to select arrival proceedure

  Scenario: Below personal minimums
    Given The flight planner
    When Any flight plan is loaded
        And I have the weather
        And PIC has entered personal mimimums
        And weather is below personal mimimums 
    Then Display warning to pilot that weather is below personal minimums
        And detail what aspect of weather is below minimums

  Scenario: Weather brief
    Given The flight planner
    When Any flight plan is loaded
        And I have the weather
    Then Display weather briefing screen to PIC
        And confirm with PIC that weather meets personal minimums

  Scenario: Smart VFR routing
    Given The flight planner
    When A VFR flight plan is beging generated
        And I have up to date airspace data
    Then Define a route which avoids
        | Temporary Flight Restrictions |
        | Terrain | 
        | Restricted Airspaces | 
        | Significant Weather | 
        And Define a route which maximizes gliding distance to safe landing areas

  Scenario: IFR routing
    Given The flight planner
    When An IFR flight plan is being generated
    Then Define an IFR route using accepted IFR routing techniques