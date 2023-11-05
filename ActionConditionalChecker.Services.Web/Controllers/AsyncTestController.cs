using ActionConditionalChecker.Contracts;
using ActionConditionalChecker.Services.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace ActionConditionalChecker.Services.Web.Controllers
{
    [Route("[controller]/[action]")]
    public class AsyncTestController : Controller
    {
        private readonly IAsyncActionConditionalChecker _checker;
        private readonly object _lock = new object();

        public AsyncTestController(IAsyncActionConditionalChecker checker)
        {
            _checker = checker;
        }

        public async Task<IActionResult> Index()
        {
            var canExecute = await _checker.CanExecuteAsync(new AsyncRequestAAccessCondition(new RequestA { Id = 3 }, 
                async x => await Task.FromResult(x.Id == 3)));

            var operationResult = await _checker.ExecuteAsync(
                new AsyncRequestAActionInfo(async request => 
                    {
                        await Task.Delay(50000);
                        Console.WriteLine("Hello!"); 
                        await Task.CompletedTask; 
                    }, 
                    new AsyncRequestAAccessCondition(new RequestA { Id = 3 }, 
                    async x => await Task.FromResult(x.Id == 3)), _lock));

            var secondOperationResult = await _checker.ExecuteAsync(
                new AsyncRequestAActionInfo(async request =>
                {
                    Console.WriteLine("Hello!");
                    await Task.CompletedTask;
                },
                new AsyncRequestAAccessCondition(new RequestA { Id = 3 }, 
                async x => await Task.FromResult(x.Id == 3)), _lock));

            if (secondOperationResult.HasFailed)
            {
                return BadRequest(secondOperationResult.Messages);
            }

            return Ok();
        }
    }
}
