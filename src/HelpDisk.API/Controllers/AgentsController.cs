using HelpDisk.API.Abstractions;
using HelpDisk.Application.Features.Agents;
using HelpDisk.Application.Features.Agents.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDisk.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public sealed class AgentsController : ApiController
{
    private readonly IAgentService _agentService;

    public AgentsController(IAgentService agentService)
    {
        _agentService = agentService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateAgentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _agentService.CreateAsync(
            request,
            cancellationToken);

        return HandleResult(result);
    }

    [HttpPut("{userId}")]
    public async Task<IActionResult> Update(
    string userId,
    UpdateAgentRequest request,
    CancellationToken cancellationToken)
    {
        var result = await _agentService.UpdateAsync(
            userId,
            request,
            cancellationToken);

        return HandleResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
    CancellationToken cancellationToken)
    {
        var result = await _agentService.GetAllAsync(
            cancellationToken);

        return HandleResult(result);
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetById(
    string userId,
    CancellationToken cancellationToken)
    {
        var result = await _agentService.GetByIdAsync(
            userId,
            cancellationToken);

        return HandleResult(result);
    }

    [HttpPost("{userId}/deactivate")]
    public async Task<IActionResult> Deactivate(
    string userId,
    CancellationToken cancellationToken)
    {
        var result = await _agentService.DeactivateAsync(
            userId,
            cancellationToken);

        return HandleResult(result);
    }

    [HttpPost("{userId}/activate")]
    public async Task<IActionResult> Activate(
        string userId,
        CancellationToken cancellationToken)
    {
        var result = await _agentService.ActivateAsync(
            userId,
            cancellationToken);

        return HandleResult(result);
    }
}