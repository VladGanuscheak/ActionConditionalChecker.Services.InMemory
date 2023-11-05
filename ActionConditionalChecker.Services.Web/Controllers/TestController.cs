using ActionConditionalChecker.Contracts;
using ActionConditionalChecker.Contracts.ActionInfo;
using ActionConditionalChecker.Services.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ActionConditionalChecker.Services.Web.Controllers
{
    [AllowAnonymous]
    [Route("[controller]/[action]")]
    public class TestController : Controller
    {
        private readonly IActionConditionalChecker _checker;

        private static readonly object _lock = new object();

        public TestController(IActionConditionalChecker checker)
        {
            _checker = checker;
        }

        public IActionResult Index()
        {
            var condition = new RequestAAccessCondition(new RequestA { Id = 2 } , x => x.Id == 2);

            var canExecute = _checker.CanExecute(condition);

            var operationResult = _checker.Execute(new RequestAActionInfo(request => { Thread.Sleep(TimeSpan.FromSeconds(50)); Console.WriteLine("Hello!"); }, condition,  _lock));

            var secondOperationResult = _checker.Execute(new RequestAActionInfo(request => { Console.WriteLine("Hello!"); }, condition, _lock));

            if (secondOperationResult.HasFailed)
            {
                return BadRequest(secondOperationResult.Messages);
            }

            return Ok();
        }
    }
}
