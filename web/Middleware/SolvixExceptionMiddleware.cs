using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Solvix.Web.Services;
namespace Solvix.Web.Middleware;
public sealed class SolvixExceptionMiddleware
{
    private readonly RequestDelegate next; private readonly ILogger<SolvixExceptionMiddleware> logger;
    public SolvixExceptionMiddleware(RequestDelegate next,ILogger<SolvixExceptionMiddleware> logger)
    {this.next=next;this.logger=logger;}
    public async Task InvokeAsync(HttpContext context)
    {
        try {await next(context);}
        catch(OperationCanceledException) when(context.RequestAborted.IsCancellationRequested) {throw;}
        catch(Exception e) when(!context.Response.HasStarted)
        {
            int status=e is ServiceException se ? se.StatusCode :
                e is FirebaseAuthException or HttpRequestException or TimeoutException ? 503 : 500;
            if(status>=500) logger.LogError("Request {TraceId} failed with {ExceptionType}.",context.TraceIdentifier,e.GetType().Name);
            context.Response.Clear();context.Response.StatusCode=status;
            context.Response.Headers["Cache-Control"]="no-store";
            if(status==401 && context.Request.Path.StartsWithSegments("/api")) context.Response.Headers["WWW-Authenticate"]="Bearer";
            var problem=new ProblemDetails{Status=status,Title=status switch {
                400=>"Invalid request",401=>"Authentication required",403=>"Access denied",404=>"Not found",
                409=>"Operation conflict",422=>"Operation cannot be completed",503=>"Service unavailable",_=>"Request failed"},
                Detail=e is ServiceException known ? known.Message : "The request could not be completed. Try again later."};
            problem.Extensions["traceId"]=context.TraceIdentifier;
            await context.Response.WriteAsJsonAsync(problem,options:null,contentType:"application/problem+json",cancellationToken:context.RequestAborted);
        }
    }
}
