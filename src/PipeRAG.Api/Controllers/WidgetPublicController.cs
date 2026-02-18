using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PipeRAG.Infrastructure.Data;

namespace PipeRAG.Api.Controllers;

/// <summary>
/// Public endpoints for the embeddable chat widget (no authentication required).
/// </summary>
[ApiController]
[Route("api/widget")]
public class WidgetPublicController : ControllerBase
{
    private readonly PipeRagDbContext _db;

    public WidgetPublicController(PipeRagDbContext db) => _db = db;

    /// <summary>
    /// Get the widget configuration for a project (public, used by the widget iframe).
    /// </summary>
    [HttpGet("{projectId:guid}/config")]
    public async Task<IActionResult> GetConfig(Guid projectId, CancellationToken ct)
    {
        var config = await _db.WidgetConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.ProjectId == projectId && w.IsActive, ct);

        if (config is null)
            return NotFound(new { error = "Widget not found or inactive." });

        // Check origin if restricted
        var origin = Request.Headers.Origin.FirstOrDefault();
        if (config.AllowedOrigins != "*" && !string.IsNullOrEmpty(origin))
        {
            var allowed = config.AllowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!allowed.Any(a => origin.Equals(a, StringComparison.OrdinalIgnoreCase)))
                return StatusCode(403, new { error = "Origin not allowed." });
        }

        return Ok(new
        {
            config.ProjectId,
            config.PrimaryColor,
            config.BackgroundColor,
            config.TextColor,
            config.Position,
            config.AvatarUrl,
            config.Title,
            config.Subtitle,
            config.PlaceholderText
        });
    }

    /// <summary>
    /// Serves the embed loader script.
    /// </summary>
    [HttpGet("embed.js")]
    [ResponseCache(Duration = 3600)]
    public IActionResult GetEmbedScript()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var js = EmbedScript.Replace("__BASE_URL__", baseUrl);
        return Content(js, "application/javascript");
    }

    /// <summary>
    /// Serves the widget iframe HTML.
    /// </summary>
    [HttpGet("{projectId:guid}/frame")]
    public async Task<IActionResult> GetFrame(Guid projectId, [FromQuery] string? apiKey, CancellationToken ct)
    {
        var config = await _db.WidgetConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.ProjectId == projectId && w.IsActive, ct);

        if (config is null)
            return NotFound("Widget not found.");

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var html = FrameHtml
            .Replace("__BASE_URL__", baseUrl)
            .Replace("__PROJECT_ID__", projectId.ToString())
            .Replace("__API_KEY__", apiKey ?? "")
            .Replace("__PRIMARY_COLOR__", config.PrimaryColor)
            .Replace("__BG_COLOR__", config.BackgroundColor)
            .Replace("__TEXT_COLOR__", config.TextColor)
            .Replace("__TITLE__", System.Web.HttpUtility.HtmlEncode(config.Title))
            .Replace("__SUBTITLE__", System.Web.HttpUtility.HtmlEncode(config.Subtitle))
            .Replace("__PLACEHOLDER__", System.Web.HttpUtility.HtmlEncode(config.PlaceholderText))
            .Replace("__AVATAR_URL__", config.AvatarUrl ?? "");

        return Content(html, "text/html");
    }

    private const string EmbedScript = @"
(function() {
  if (window.__piperag_widget_loaded) return;
  window.__piperag_widget_loaded = true;

  var script = document.currentScript;
  var projectId = script.getAttribute('data-project-id');
  var apiKey = script.getAttribute('data-api-key') || '';
  var position = script.getAttribute('data-position') || 'bottom-right';
  var baseUrl = '__BASE_URL__';

  // Create toggle button
  var btn = document.createElement('div');
  btn.id = 'piperag-widget-btn';
  btn.innerHTML = '<svg width=""24"" height=""24"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2""><path d=""M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z""/></svg>';
  btn.style.cssText = 'position:fixed;' + (position === 'bottom-left' ? 'left:20px' : 'right:20px') + ';bottom:20px;width:56px;height:56px;border-radius:50%;background:#6366f1;color:#fff;display:flex;align-items:center;justify-content:center;cursor:pointer;box-shadow:0 4px 12px rgba(0,0,0,0.3);z-index:99999;transition:transform 0.2s';
  btn.onmouseenter = function() { btn.style.transform = 'scale(1.1)'; };
  btn.onmouseleave = function() { btn.style.transform = 'scale(1)'; };

  // Create iframe container
  var container = document.createElement('div');
  container.id = 'piperag-widget-container';
  container.style.cssText = 'position:fixed;' + (position === 'bottom-left' ? 'left:20px' : 'right:20px') + ';bottom:90px;width:400px;height:600px;max-height:80vh;border-radius:16px;overflow:hidden;box-shadow:0 8px 32px rgba(0,0,0,0.4);z-index:99999;display:none;';

  var iframe = document.createElement('iframe');
  iframe.src = baseUrl + '/api/widget/' + projectId + '/frame?apiKey=' + encodeURIComponent(apiKey);
  iframe.style.cssText = 'width:100%;height:100%;border:none;';
  iframe.setAttribute('sandbox', 'allow-scripts allow-same-origin allow-forms');
  container.appendChild(iframe);

  document.body.appendChild(btn);
  document.body.appendChild(container);

  var open = false;
  btn.onclick = function() {
    open = !open;
    container.style.display = open ? 'block' : 'none';
    btn.innerHTML = open
      ? '<svg width=""24"" height=""24"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2""><line x1=""18"" y1=""6"" x2=""6"" y2=""18""/><line x1=""6"" y1=""6"" x2=""18"" y2=""18""/></svg>'
      : '<svg width=""24"" height=""24"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2""><path d=""M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z""/></svg>';
  };

  // Fetch config to apply theme to button
  fetch(baseUrl + '/api/widget/' + projectId + '/config')
    .then(function(r) { return r.json(); })
    .then(function(cfg) {
      btn.style.background = cfg.primaryColor || '#6366f1';
      if (cfg.position === 'bottom-left') {
        btn.style.left = '20px'; btn.style.right = 'auto';
        container.style.left = '20px'; container.style.right = 'auto';
      }
    }).catch(function() {});
})();
";

    private const string FrameHtml = @"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width,initial-scale=1"">
<title>PipeRAG Chat</title>
<style>
*{margin:0;padding:0;box-sizing:border-box}
body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;background:__BG_COLOR__;color:__TEXT_COLOR__;height:100vh;display:flex;flex-direction:column}
.header{padding:16px 20px;background:__PRIMARY_COLOR__;color:#fff;flex-shrink:0}
.header h3{font-size:16px;font-weight:600}
.header p{font-size:12px;opacity:0.8;margin-top:2px}
.avatar{width:32px;height:32px;border-radius:50%;margin-right:12px;object-fit:cover}
.header-inner{display:flex;align-items:center}
.messages{flex:1;overflow-y:auto;padding:16px;display:flex;flex-direction:column;gap:12px}
.msg{max-width:85%;padding:10px 14px;border-radius:12px;font-size:14px;line-height:1.5;word-wrap:break-word}
.msg.user{align-self:flex-end;background:__PRIMARY_COLOR__;color:#fff;border-bottom-right-radius:4px}
.msg.assistant{align-self:flex-start;background:rgba(255,255,255,0.1);border-bottom-left-radius:4px}
.msg.typing{opacity:0.6;font-style:italic}
.input-area{padding:12px 16px;border-top:1px solid rgba(255,255,255,0.1);flex-shrink:0;display:flex;gap:8px}
.input-area input{flex:1;padding:10px 14px;border:1px solid rgba(255,255,255,0.2);border-radius:24px;background:rgba(255,255,255,0.05);color:__TEXT_COLOR__;font-size:14px;outline:none}
.input-area input:focus{border-color:__PRIMARY_COLOR__}
.input-area button{padding:10px 18px;border:none;border-radius:24px;background:__PRIMARY_COLOR__;color:#fff;font-size:14px;cursor:pointer;font-weight:500}
.input-area button:hover{opacity:0.9}
.input-area button:disabled{opacity:0.5;cursor:not-allowed}
.powered{text-align:center;padding:8px;font-size:11px;opacity:0.4}
</style>
</head>
<body>
<div class=""header"">
  <div class=""header-inner"">
    <img class=""avatar"" src=""__AVATAR_URL__"" onerror=""this.style.display='none'"" alt="""">
    <div><h3>__TITLE__</h3><p>__SUBTITLE__</p></div>
  </div>
</div>
<div class=""messages"" id=""messages""></div>
<div class=""input-area"">
  <input type=""text"" id=""input"" placeholder=""__PLACEHOLDER__"" autocomplete=""off"">
  <button id=""send"">Send</button>
</div>
<div class=""powered"">Powered by PipeRAG</div>
<script>
(function(){
  var baseUrl='__BASE_URL__', projectId='__PROJECT_ID__', apiKey='__API_KEY__';
  var messagesEl=document.getElementById('messages'), input=document.getElementById('input'), sendBtn=document.getElementById('send');
  var sessionId=null, sending=false;

  function addMsg(role, text){
    var d=document.createElement('div');
    d.className='msg '+role;
    d.textContent=text;
    messagesEl.appendChild(d);
    messagesEl.scrollTop=messagesEl.scrollHeight;
    return d;
  }

  async function send(){
    var text=input.value.trim();
    if(!text||sending)return;
    sending=true; sendBtn.disabled=true; input.value='';
    addMsg('user',text);
    var typingEl=addMsg('assistant','Thinking...');
    typingEl.classList.add('typing');

    try{
      var headers={'Content-Type':'application/json'};
      if(apiKey) headers['Authorization']='Bearer '+apiKey;
      var body=JSON.stringify({message:text,sessionId:sessionId});
      var res=await fetch(baseUrl+'/api/projects/'+projectId+'/chat/stream',{method:'POST',headers:headers,body:body});
      if(!res.ok){typingEl.textContent='Error: '+res.status;sending=false;sendBtn.disabled=false;return;}

      var reader=res.body.getReader(), decoder=new TextDecoder(), fullText='', buffer='';
      typingEl.textContent='';typingEl.classList.remove('typing');

      while(true){
        var result=await reader.read();
        if(result.done)break;
        buffer+=decoder.decode(result.value,{stream:true});
        var lines=buffer.split('\n');
        buffer=lines.pop()||'';
        for(var i=0;i<lines.length;i++){
          var line=lines[i].trim();
          if(!line.startsWith('data:'))continue;
          try{
            var chunk=JSON.parse(line.substring(5));
            if(chunk.sessionId)sessionId=chunk.sessionId;
            if(chunk.content){fullText+=chunk.content;typingEl.textContent=fullText;}
          }catch(e){}
        }
      }
      if(!fullText)typingEl.textContent='No response.';
    }catch(e){typingEl.textContent='Connection error.';}
    sending=false;sendBtn.disabled=false;input.focus();
  }

  sendBtn.onclick=send;
  input.onkeydown=function(e){if(e.key==='Enter')send();};
  input.focus();
})();
</script>
</body>
</html>";
}
