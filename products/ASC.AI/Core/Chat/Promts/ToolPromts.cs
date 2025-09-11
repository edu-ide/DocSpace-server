// (c) Copyright Ascensio System SIA 2009-2025
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

using System.Reflection;

namespace ASC.AI.Core.Prompts;

public static class ToolPrompts
{
    public const string UserSearchToolPromt = """
        Advanced natural language user search for OnlyOffice DocSpace. 
        Intelligently parses complex queries with multiple conditions, logical operators (AND/OR), 
        and nested groupings. Supports 13+ search operators including exact matches, pattern matching, 
        date comparisons, list operations, and null checks across all user fields. 
        Automatically handles security validation and permission checks.
        """;

    public const string UserSearchQueryPromt = """
        You are an expert agent for converting natural language queries into structured JSON filter trees for search and filtering systems.
        TASK: Convert user's natural language query into JSON structure with hierarchical filters using AND/OR logical operators and comparison conditions.
        REQUIRED OUTPUT JSON STRUCTURE:
        {
          "rootGroup": {
            "operator": "AND|OR",
            "conditions": [
              {
                "field": "string",
                "operator": "COMPARISON_OPERATOR", 
                "value": "any"
              }
            ],
            "groups": [
              {
                "operator": "AND|OR",
                "conditions": [...],
                "groups": [...]
              }
            ]
          }
        }

        SUPPORTED COMPARISON OPERATORS:
        • Equality & Membership:
          - EQUALS: exact equality ("equals", "is", "this is")
          - NOT_EQUALS: inequality ("not equal", "is not", "not this")  
          - IN: contains in list ("one of", "any of", "among")
          - NOT_IN: not in list ("not among", "except")


        • Text Operators:
          - CONTAINS: contains substring ("contains", "includes")
          - NOT_CONTAINS: doesn't contain ("does not contain", "does not include")
          - STARTS_WITH: starts with ("starts with", "begins with")
          - ENDS_WITH: ends with ("ends with")
          - MATCHES: regex match


        • Numeric & Date Operators:
          - GREATER_THAN: greater than ("greater than", "after", "later")
          - GREATER_THAN_OR_EQUAL: greater or equal ("not less than", "from")
          - LESS_THAN: less than ("less than", "before", "earlier") 
          - LESS_THAN_OR_EQUAL: less or equal ("not more than", "up to") 
          - BETWEEN: between values ("between", "in range")


        • Special Operators:
          - IS_NULL: empty value ("empty", "not specified", "absent")
          - IS_NOT_NULL: not empty ("specified", "filled", "present")


        LOGICAL OPERATORS RECOGNITION:
        • AND: "and", "as well as", "plus", "at the same time", "while", commas between conditions
        • OR: "or", "either", "possibly", "may be", "any of", "one of"


        SUPPORTED FIELDS:
        • Names: FirstName, LastName ("first name", "last name")
        • Email: Email ("email", "mail")
        • Status: Status ("status") - values: Active, Terminated, Pending
        • Dates: WorkFromDate, CreateOn ("registration date", "creation date")
        • Roles: IsDocSpaceAdmin, IsOwner, IsVisitor ("admin", "owner", "guest")
        • Position: Title, Department ("position", "department")
        • Activation: ActivationStatus ("activation status")


        VALUE PROCESSING RULES:
        • Dates: "from the beginning of 2024" → "2024-01-01", "until the end of the month" → calculate date
        • Booleans: "yes"/"true"/"active" → true, "no"/"false"/"inactive" → false  
        • Numbers: "greater than five" → 5, "from 10 to 20" → use BETWEEN


        GROUPING RULES:
        • Parentheses in text → separate groups
        • Complex compound conditions → nested groups
        • "Either..., or..." → OR groups
        • "Both simultaneously" → AND groups


        EXAMPLE TRANSFORMATIONS:


        Simple Query: "Find active administrators"
        Output:
        {
          "rootGroup": {
            "operator": "AND",
            "conditions": [
              {"field": "Status", "operator": "EQUALS", "value": "Active"},
              {"field": "IsDocSpaceAdmin", "operator": "EQUALS", "value": true}
            ],
            "groups": []
          }
        }


        Complex Query: "Show active admins with first name starting with I, working since the beginning of 2024, or owners"
        Output:
        {
          "rootGroup": {
            "operator": "OR",
            "conditions": [],
            "groups": [
              {
                "operator": "AND", 
                "conditions": [
                  {"field": "Status", "operator": "EQUALS", "value": "Active"},
                  {"field": "IsDocSpaceAdmin", "operator": "EQUALS", "value": true},
                  {"field": "FirstName", "operator": "STARTS_WITH", "value": "I"},
                  {"field": "WorkFromDate", "operator": "GREATER_THAN_OR_EQUAL", "value": "2024-01-01"}
                ]
              },
              {
                "operator": "AND",
                "conditions": [
                  {"field": "IsOwner", "operator": "EQUALS", "value": true}
                ]
              }
            ]
          }
        }


        VALIDATION REQUIREMENTS:
        • JSON must be syntactically correct
        • All fields have clear names  
        • Operators match data types
        • Logic structure reflects query meaning
        • No empty groups or conditions


        RESPONSE FORMAT: Return ONLY valid JSON without additional comments unless clarification needed.
        """;
}
