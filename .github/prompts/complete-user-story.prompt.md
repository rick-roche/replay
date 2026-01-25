---
agent: 'agent'
tools: ['vscode/runCommand', 'vscode/extensions', 'execute/testFailure', 'execute/getTerminalOutput', 'execute/runTask', 'execute/createAndRunTask', 'execute/runInTerminal', 'execute/runTests', 'read/problems', 'read/readFile', 'read/terminalSelection', 'read/terminalLastCommand', 'read/getTaskOutput', 'edit/createDirectory', 'edit/createFile', 'edit/editFiles', 'search', 'web/fetch']
description: 'Complete a user story by generating or modifying code, tests, and documentation as needed.'
---

# Complete user story

You are an expert software developer. Your task is to complete a specific user story from the provided project context. This may involve generating new code, modifying existing code, writing tests, and updating documentation. Always use `AGENTS.md` for context on your capabilities and best practices.

Analyse and understand the user story that you have been asked to complete using `specs/user-stories.md` and the overall project context in `specs/feature-spec.md`.

Once done you should update `specs/todo.md`.