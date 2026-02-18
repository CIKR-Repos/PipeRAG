using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PipeRAG.Core.DTOs;
using PipeRAG.Core.Entities;
using PipeRAG.Infrastructure.Data;

namespace PipeRAG.Api.Controllers;

/// <summary>
/// Public endpoints for the embeddable chat widget (no auth required, uses project API key).
/// </summary>
[ApiController]
[Route("api/widget")]
public class PublicWidgetController : ControllerBase
{
    private readonly PipeRagDbContext _db;

    public PublicWidgetController(PipeRagDbContext db) => _db = db;

    /// <summary>
    /// Get widget configuration by project ID and API key.
    /// Used by the embedded widget to load its theme/settings.
    /// </summary>
    [HttpGet("{projectId:guid}/config")]
    public async Task<IActionResult> GetConfig(Guid projectId, [FromQuery] string apiKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return BadRequest(new { error = "API key is required." });

        // Validate the API key belongs to the project owner
        var project = await _db.Projects.FindAsync([projectId], ct);
        if (project is null)
            return NotFound(new { error = "Project not found." });

        var keyPrefix = apiKey.Length >= 8 ? apiKey.Substring(0, 8) : apiKey;
        var validKey = await _db.ApiKeys
            .AnyAsync(k => k.KeyPrefix == keyPrefix && k.UserId == project.OwnerId && k.IsActive, ct);
        if (!validKey)
            return Unauthorized(new { error = "Invalid API key." });

        var config = await _db.WidgetConfigs.FirstOrDefaultAsync(w => w.ProjectId == projectId, ct);
        if (config is null || !config.IsActive)
            return NotFound(new { error = "Widget not configured or disabled." });

        // Check origin if AllowedOrigins is not wildcard
        var origin = Request.Headers.Origin.ToString();
        if (config.AllowedOrigins != "*" && !string.IsNullOrEmpty(origin))
        {
            var allowed = config.AllowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!allowed.Any(a => a.Equals(origin, StringComparison.OrdinalIgnoreCase)))
                return StatusCode(403, new { error = "Origin not allowed." });
        }

        return Ok(new WidgetPublicConfigResponse(
            config.ProjectId,
            config.PrimaryColor,
            config.BackgroundColor,
            config.TextColor,
            config.Position,
            config.AvatarUrl,
            config.Title,
            config.Subtitle,
            config.PlaceholderText));
    }

    /// <summary>
    /// Public chat endpoint for the widget. Sends a message and returns a response.
    /// </summary>
    [HttpPost("{projectId:guid}/chat")]
    public async Task<IActionResult> Chat(Guid projectId, [FromQuery] string apiKey, [FromBody] WidgetChatRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return BadRequest(new { error = "API key is required." });

        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = "Message is required." });

        var project = await _db.Projects.FindAsync([projectId], ct);
        if (project is null)
            return NotFound(new { error = "Project not found." });

        var chatKeyPrefix = apiKey.Length >= 8 ? apiKey.Substring(0, 8) : apiKey;
        var validKey = await _db.ApiKeys
            .AnyAsync(k => k.KeyPrefix == chatKeyPrefix && k.UserId == project.OwnerId && k.IsActive, ct);
        if (!validKey)
            return Unauthorized(new { error = "Invalid API key." });

        var config = await _db.WidgetConfigs.FirstOrDefaultAsync(w => w.ProjectId == projectId, ct);
        if (config is null || !config.IsActive)
            return NotFound(new { error = "Widget not configured or disabled." });

        // For now, return a placeholder. In production this would call QueryEngineService.
        // The widget chat uses the same RAG pipeline as the main chat.
        return Ok(new WidgetChatResponse(
            "I'm the PipeRAG assistant. The chat widget is connected successfully! Full RAG responses will be available once the pipeline is configured.",
            DateTime.UtcNow));
    }

    /// <summary>
    /// Serves the embeddable widget loader script.
    /// </summary>
    [HttpGet("embed.js")]
    [Produces("application/javascript")]
    public IActionResult GetEmbedScript()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var script = GenerateEmbedScript(baseUrl);
        return Content(script, "application/javascript");
    }

    private static string GenerateEmbedScript(string baseUrl) => $$"""
    (function() {
      'use strict';
      if (window.__piperag_widget_loaded) return;
      window.__piperag_widget_loaded = true;

      var config = window.PipeRAGWidget || {};
      var projectId = config.projectId;
      var apiKey = config.apiKey;
      if (!projectId || !apiKey) {
        console.error('PipeRAG Widget: projectId and apiKey are required.');
        return;
      }

      var iframe = document.createElement('iframe');
      iframe.id = 'piperag-widget-frame';
      iframe.src = '{{baseUrl}}/api/widget/' + projectId + '/frame?apiKey=' + encodeURIComponent(apiKey);
      iframe.style.cssText = 'position:fixed;bottom:20px;right:20px;width:0;height:0;border:none;z-index:999999;opacity:0;transition:all 0.3s ease;border-radius:16px;box-shadow:0 8px 32px rgba(0,0,0,0.15);';
      iframe.setAttribute('sandbox', 'allow-scripts allow-same-origin allow-forms');
      iframe.setAttribute('allow', 'clipboard-write');
      document.body.appendChild(iframe);

      var btn = document.createElement('div');
      btn.id = 'piperag-widget-btn';
      btn.innerHTML = '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/></svg>';
      btn.style.cssText = 'position:fixed;bottom:20px;right:20px;width:56px;height:56px;border-radius:50%;background:#6366f1;color:white;display:flex;align-items:center;justify-content:center;cursor:pointer;z-index:1000000;box-shadow:0 4px 12px rgba(0,0,0,0.15);transition:transform 0.2s ease;';
      document.body.appendChild(btn);

      var open = false;
      btn.addEventListener('click', function() {
        open = !open;
        if (open) {
          iframe.style.width = (config.width || 400) + 'px';
          iframe.style.height = (config.height || 600) + 'px';
          iframe.style.opacity = '1';
          btn.style.transform = 'scale(0)';
        } else {
          iframe.style.width = '0';
          iframe.style.height = '0';
          iframe.style.opacity = '0';
          btn.style.transform = 'scale(1)';
        }
      });

      window.addEventListener('message', function(e) {
        if (e.data && e.data.type === 'piperag-close') {
          open = false;
          iframe.style.width = '0';
          iframe.style.height = '0';
          iframe.style.opacity = '0';
          btn.style.transform = 'scale(1)';
        }
        if (e.data && e.data.type === 'piperag-loaded' && e.data.config) {
          btn.style.background = e.data.config.primaryColor || '#6366f1';
          if (e.data.config.position === 'bottom-left') {
            btn.style.right = 'auto';
            btn.style.left = '20px';
            iframe.style.right = 'auto';
            iframe.style.left = '20px';
          }
        }
      });

      // Fetch config to apply theme to button
      fetch('{{baseUrl}}/api/widget/' + projectId + '/config?apiKey=' + encodeURIComponent(apiKey))
        .then(function(r) { return r.json(); })
        .then(function(cfg) {
          if (cfg.primaryColor) btn.style.background = cfg.primaryColor;
          if (cfg.position === 'bottom-left') {
            btn.style.right = 'auto';
            btn.style.left = '20px';
            iframe.style.right = 'auto';
            iframe.style.left = '20px';
          }
        })
        .catch(function() {});
    })();
    """;

    /// <summary>
    /// Serves the widget iframe HTML page.
    /// </summary>
    [HttpGet("{projectId:guid}/frame")]
    public async Task<IActionResult> GetFrame(Guid projectId, [FromQuery] string apiKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return BadRequest("Missing API key");

        var project = await _db.Projects.FindAsync([projectId], ct);
        if (project is null)
            return NotFound("Project not found");

        var config = await _db.WidgetConfigs.FirstOrDefaultAsync(w => w.ProjectId == projectId, ct);
        var primaryColor = config?.PrimaryColor ?? "#6366f1";
        var bgColor = config?.BackgroundColor ?? "#1e1e2e";
        var textColor = config?.TextColor ?? "#ffffff";
        var title = config?.Title ?? "Chat with us";
        var subtitle = config?.Subtitle ?? "Ask anything";
        var placeholder = config?.PlaceholderText ?? "Type a message...";
        var avatarUrl = config?.AvatarUrl ?? "";
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var avatarHtml = string.IsNullOrEmpty(avatarUrl)
            ? "<svg width='20' height='20' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2'><path d='M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z'/></svg>"
            : $"<img src='{avatarUrl}' alt='avatar'/>";
        var position = config?.Position ?? "bottom-right";

        var html = @"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""utf-8""/>
<meta name=""viewport"" content=""width=device-width,initial-scale=1""/>
<style>
*{margin:0;padding:0;box-sizing:border-box}
body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;background:__BG__;color:__TEXT__;height:100vh;display:flex;flex-direction:column}
.header{padding:16px 20px;background:__PRIMARY__;display:flex;align-items:center;gap:12px}
.header-avatar{width:36px;height:36px;border-radius:50%;background:rgba(255,255,255,0.2);display:flex;align-items:center;justify-content:center;overflow:hidden}
.header-avatar img{width:100%;height:100%;object-fit:cover}
.header-text h3{font-size:15px;font-weight:600;color:#fff}
.header-text p{font-size:12px;color:rgba(255,255,255,0.8)}
.close-btn{margin-left:auto;background:none;border:none;color:rgba(255,255,255,0.8);cursor:pointer;font-size:20px;padding:4px 8px}
.close-btn:hover{color:#fff}
.messages{flex:1;overflow-y:auto;padding:16px;display:flex;flex-direction:column;gap:8px}
.msg{max-width:85%;padding:10px 14px;border-radius:12px;font-size:14px;line-height:1.5;word-wrap:break-word}
.msg-user{align-self:flex-end;background:__PRIMARY__;color:#fff;border-bottom-right-radius:4px}
.msg-bot{align-self:flex-start;background:rgba(255,255,255,0.1);border-bottom-left-radius:4px}
.input-area{padding:12px 16px;border-top:1px solid rgba(255,255,255,0.1);display:flex;gap:8px}
.input-area input{flex:1;background:rgba(255,255,255,0.1);border:1px solid rgba(255,255,255,0.15);border-radius:8px;padding:10px 14px;color:__TEXT__;font-size:14px;outline:none}
.input-area input::placeholder{color:rgba(255,255,255,0.4)}
.input-area input:focus{border-color:__PRIMARY__}
.send-btn{background:__PRIMARY__;border:none;color:#fff;padding:10px 16px;border-radius:8px;cursor:pointer;font-weight:600;font-size:14px}
.send-btn:disabled{opacity:0.5;cursor:not-allowed}
.typing{align-self:flex-start;padding:10px 14px;background:rgba(255,255,255,0.1);border-radius:12px;font-size:13px;color:rgba(255,255,255,0.5)}
</style>
</head>
<body>
<div class=""header"">
<div class=""header-avatar"">__AVATAR__</div>
<div class=""header-text""><h3>__TITLE__</h3><p>__SUBTITLE__</p></div>
<button class=""close-btn"" onclick=""parent.postMessage({type:'piperag-close'},'*')"">✕</button>
</div>
<div class=""messages"" id=""messages""><div class=""msg msg-bot"">Hi! How can I help you today?</div></div>
<div class=""input-area"">
<input type=""text"" id=""input"" placeholder=""__PLACEHOLDER__"" autocomplete=""off""/>
<button class=""send-btn"" id=""sendBtn"" onclick=""send()"">Send</button>
</div>
<script>
var projectId='__PROJECTID__';var apiKey='__APIKEY__';var baseUrl='__BASEURL__';
var input=document.getElementById('input'),msgs=document.getElementById('messages'),sendBtn=document.getElementById('sendBtn'),sending=false;
parent.postMessage({type:'piperag-loaded',config:{primaryColor:'__PRIMARY__',position:'__POSITION__'}},'*');
input.addEventListener('keydown',function(e){if(e.key==='Enter'&&!e.shiftKey){e.preventDefault();send();}});
function addMsg(t,r){var d=document.createElement('div');d.className='msg msg-'+r;d.textContent=t;msgs.appendChild(d);msgs.scrollTop=msgs.scrollHeight;return d;}
function send(){var t=input.value.trim();if(!t||sending)return;sending=true;sendBtn.disabled=true;input.value='';addMsg(t,'user');
var typ=document.createElement('div');typ.className='typing';typ.textContent='Thinking...';msgs.appendChild(typ);msgs.scrollTop=msgs.scrollHeight;
fetch(baseUrl+'/api/widget/'+projectId+'/chat?apiKey='+encodeURIComponent(apiKey),{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({message:t})})
.then(function(r){return r.json();}).then(function(d){msgs.removeChild(typ);addMsg(d.response||d.error||'No response','bot');})
.catch(function(){msgs.removeChild(typ);addMsg('Error: Could not reach server.','bot');})
.finally(function(){sending=false;sendBtn.disabled=false;input.focus();});}
</script>
</body></html>"
            .Replace("__PRIMARY__", primaryColor)
            .Replace("__BG__", bgColor)
            .Replace("__TEXT__", textColor)
            .Replace("__TITLE__", title)
            .Replace("__SUBTITLE__", subtitle)
            .Replace("__PLACEHOLDER__", placeholder)
            .Replace("__AVATAR__", avatarHtml)
            .Replace("__PROJECTID__", projectId.ToString())
            .Replace("__APIKEY__", apiKey)
            .Replace("__BASEURL__", baseUrl)
            .Replace("__POSITION__", position);

        return Content(html, "text/html");
    }
}

public record WidgetPublicConfigResponse(
    Guid ProjectId,
    string PrimaryColor,
    string BackgroundColor,
    string TextColor,
    string Position,
    string? AvatarUrl,
    string Title,
    string Subtitle,
    string PlaceholderText);

public record WidgetChatRequest(string Message);
public record WidgetChatResponse(string Response, DateTime Timestamp);
