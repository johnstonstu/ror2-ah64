# Optional local MCP tools

The mod builds without MCP. `.mcp.example.json` describes the optional Blender and
Unity servers used during development.

Copy it to `.mcp.json`, which is ignored, then set the Blender MCP checkout path.
Install `uv`/`uvx` on PATH or use their absolute paths in your private copy. The
example's Blender port is 9877; it must match the running Blender Lab extension.
Use the official Blender Lab server, not the unrelated third-party implementation.
The Blender bridge needs an open GUI session.

Unity uses the standalone 2021.3.33f1 editor. Pin its project path. Use project-scoped
servers and avoid same-named global registrations that shadow them. Reload the
assistant session after adding or changing servers.

The example permits online resolution for setup. Once cached, `--offline` can be
added to the Unity server arguments; rewarm the cache online if resolution later
fails. Do not commit machine settings, credentials, logs or assistant session state.
