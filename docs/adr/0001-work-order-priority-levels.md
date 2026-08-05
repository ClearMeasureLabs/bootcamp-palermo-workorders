# ADR 0001: Work Order Priority Levels

**Status:** Proposed  
**Date:** 2026-08-05  
**Epic:** #8227

## Context

The church maintenance staff currently manages work orders without a formal priority system. All work orders are treated equally, making it difficult to triage urgent issues (broken HVAC, safety hazards) versus routine maintenance tasks. Staff must manually track which items need immediate attention, leading to potential delays in addressing critical issues.

## Decision

We will add a priority classification system to work orders with four levels:
- **Urgent**: Safety hazards, critical system failures requiring immediate attention
- **High**: Important issues affecting operations but not immediately critical
- **Normal**: Standard maintenance and routine repairs (default)
- **Low**: Nice-to-have improvements and non-urgent tasks

## Conceptual Definition

### Business Value
Church staff can prioritize urgent repairs (broken HVAC, safety hazards) over routine maintenance, ensuring critical issues are addressed first. This improves response times for critical issues and helps staff allocate resources effectively.

### User Stories
1. As a maintenance coordinator, I want to mark work orders as Urgent so that staff know which issues require immediate attention
2. As a maintenance worker, I want to see work orders sorted by priority so I can address the most critical issues first
3. As a facilities manager, I want to filter work orders by priority level so I can review all urgent items at once

### Conceptual Model
```
WorkOrder
├── Priority (enum: Urgent, High, Normal, Low)
├── [existing fields...]
└── Visual indicators (colors/icons) for each priority level
```

### Priority Level Definitions

**Urgent (Red)**
- Safety hazards (exposed wiring, gas leaks, structural damage)
- Critical system failures (HVAC in extreme weather, no water, power outage)
- Security issues (broken locks, alarm system failures)
- Response time: Same day

**High (Orange)**
- Important operational issues (broken sound system before service, leaking roof)
- Equipment failures affecting ministry activities
- Issues affecting multiple areas or people
- Response time: Within 2-3 days

**Normal (Blue)**
- Standard maintenance requests
- Routine repairs
- Cosmetic issues
- Response time: Within 1-2 weeks

**Low (Gray)**
- Nice-to-have improvements
- Non-urgent cosmetic updates
- Future enhancements
- Response time: As time permits

### UI/UX Considerations
- Priority dropdown on create/edit forms with clear descriptions
- Color-coded priority badges on work order cards
- Default priority: Normal (to avoid forcing users to make a choice)
- Sort work orders by priority (Urgent → High → Normal → Low)
- Filter capability to show only specific priority levels

## Architecture Diagrams

### Before: Current Work Order Model
![Before](0001-before.png)

### After: Work Order Model with Priority
![After](0001-after.png)

## Consequences

### Positive
- Clear prioritization helps staff focus on critical issues first
- Improved response times for urgent matters
- Better resource allocation and scheduling
- Visual indicators make priority immediately obvious
- Filtering and sorting capabilities improve workflow efficiency

### Negative
- Requires training staff on when to use each priority level
- Risk of "priority inflation" (marking everything as Urgent)
- Additional field to maintain in the database
- Existing work orders will need default priority assigned

### Mitigation
- Provide clear guidelines and examples for each priority level
- Monitor priority usage patterns and provide feedback to staff
- Consider requiring justification for Urgent priority
- Migration script will set all existing work orders to Normal priority

## Implementation Notes

This epic decomposes into three child issues:
1. Add WorkOrderPriority enum to Core model (#8264)
2. Add priority field to work order UI forms (#8265)
3. Add priority sorting and filtering to work order list (#8266)

## UX Design

### Visual Design System

**Priority Badges**
- Urgent: Red badge with exclamation icon (⚠️)
- High: Orange badge with up arrow (↑)
- Normal: Blue badge with equals sign (=)
- Low: Gray badge with down arrow (↓)

**Color Palette**
```
Urgent:  #DC2626 (red-600)
High:    #EA580C (orange-600)
Normal:  #2563EB (blue-600)
Low:     #6B7280 (gray-500)
```

### UI Components

**Priority Dropdown (Create/Edit Forms)**
```
┌─────────────────────────────────┐
│ Priority *                      │
│ ┌─────────────────────────────┐ │
│ │ ⚠️  Urgent                   ▼│ │
│ └─────────────────────────────┘ │
│                                 │
│ Options:                        │
│ ⚠️  Urgent - Safety hazards     │
│ ↑  High - Important issues      │
│ =  Normal - Standard (default)  │
│ ↓  Low - Nice-to-have          │
└─────────────────────────────────┘
```

**Work Order Card with Priority**
```
┌────────────────────────────────────┐
│ ⚠️ WO-123: Broken HVAC System     │
│ Status: Assigned  Priority: Urgent│
│ Assignee: John Smith              │
│ Created: 2026-08-05               │
└────────────────────────────────────┘
```

**Work Order List with Priority Column**
```
┌──────────┬────────────────┬──────────┬──────────┐
│ Priority │ Number         │ Title    │ Status   │
├──────────┼────────────────┼──────────┼──────────┤
│ ⚠️ Urgent│ WO-123        │ HVAC...  │ Assigned │
│ ⚠️ Urgent│ WO-125        │ Gas...   │ Draft    │
│ ↑ High   │ WO-124        │ Roof...  │ Progress │
│ = Normal │ WO-122        │ Paint... │ Draft    │
└──────────┴────────────────┴──────────┴──────────┘
```

### User Interactions

**Priority Selection Flow**
1. User opens create/edit work order form
2. Priority dropdown defaults to "Normal"
3. User clicks dropdown to see all options with descriptions
4. User selects appropriate priority level
5. Visual badge updates immediately to show selected priority
6. Form validation ensures priority is set before save

**Filtering & Sorting**
- Filter dropdown above work order list: "All Priorities", "Urgent", "High", "Normal", "Low"
- Default sort: Priority (Urgent → High → Normal → Low), then by Created Date (newest first)
- Click priority column header to toggle sort direction
- Active filters shown as removable chips

### Accessibility Considerations
- Color is not the only indicator (icons + text labels)
- ARIA labels for screen readers: "Priority: Urgent - requires immediate attention"
- Keyboard navigation support for priority dropdown
- High contrast mode support
- Focus indicators on interactive elements

### Mobile Responsiveness
- Priority badges scale appropriately on small screens
- Dropdown touch targets minimum 44x44px
- Priority column visible on mobile (essential information)
- Swipe gestures for filtering on mobile

## References
- Epic Issue: #8227
- Child Issues: #8264, #8265, #8266
