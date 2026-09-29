using CareHome.Api.Dtos.FundingContracts;
using CareHome.Api.Funding;
using CareHome.Api.Security;
using Microsoft.AspNetCore.Mvc;

namespace CareHome.Api.Controllers;

[ApiController]
[RequireTenant]
public class FundingContractsController(
    FundingContractService fundingContracts,
    ITenantContext tenantContext) : ControllerBase
{
    [HttpGet("api/clients/{clientId:int}/funding-contracts")]
    public async Task<ActionResult<List<FundingContractDto>>> GetForClient(int clientId)
    {
        var contracts = await fundingContracts.ListForClientAsync(tenantContext.TenantId, clientId);
        return contracts is null ? NotFound() : Ok(contracts);
    }

    [HttpPost("api/clients/{clientId:int}/funding-contracts")]
    public async Task<ActionResult<FundingContractDto>> Create(int clientId, CreateFundingContractRequest request)
    {
        var (contract, error, clientMissing) = await fundingContracts.CreateAsync(
            tenantContext.TenantId,
            clientId,
            request);

        if (clientMissing)
        {
            return NotFound();
        }

        if (error is not null)
        {
            return error.Code is FundingContractOverlap.ConflictCode
                ? BadRequest(new { message = error.Message, code = error.Code })
                : BadRequest(new { message = error.Message });
        }

        return CreatedAtAction(nameof(GetOne), new { id = contract!.Id }, contract);
    }

    [HttpGet("api/funding-contracts/{id:int}")]
    public async Task<ActionResult<FundingContractDto>> GetOne(int id)
    {
        var dto = await fundingContracts.GetAsync(tenantContext.TenantId, id);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPut("api/funding-contracts/{id:int}")]
    public async Task<ActionResult<FundingContractDto>> Update(int id, UpdateFundingContractRequest request)
    {
        var (contract, error, missing) = await fundingContracts.UpdateAsync(
            tenantContext.TenantId,
            id,
            request);

        if (missing)
        {
            return NotFound();
        }

        if (error is not null)
        {
            return error.Code is FundingContractOverlap.ConflictCode
                ? BadRequest(new { message = error.Message, code = error.Code })
                : BadRequest(new { message = error.Message });
        }

        return Ok(contract);
    }

    [HttpGet("api/funding-contracts/{id:int}/rates")]
    public async Task<ActionResult<List<FundingRateDto>>> GetRates(int id)
    {
        var rates = await fundingContracts.ListRatesAsync(tenantContext.TenantId, id);
        return rates is null ? NotFound() : Ok(rates);
    }

    [HttpPost("api/funding-contracts/{id:int}/rates")]
    public async Task<ActionResult<FundingRateDto>> AddRate(int id, CreateFundingRateRequest request)
    {
        var (rate, error, missing) = await fundingContracts.AddRateAsync(
            tenantContext.TenantId,
            id,
            request);

        if (missing)
        {
            return NotFound();
        }

        if (error is not null)
        {
            return BadRequest(new { message = error.Message });
        }

        return Ok(rate);
    }
}
