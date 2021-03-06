﻿using MFS.Controllers.Attributes;
using MFS.Hubs;
using MFS.Models;
using MFS.Repositorys;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Senparc.Weixin.Work.AdvancedAPIs;
using Senparc.Weixin.Work.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MFS.Controllers
{
    [Route("api/[controller]"), Axios]
    public class SalesPlanController : ControllerBase
    {
        private readonly SalesPlanRepository r;
        private readonly ClientRepository cr;
        private readonly IHubContext<PrintHub> _hub;
        WorkOption option;
        public SalesPlanController(SalesPlanRepository repository, IHubContext<PrintHub> hub, IOptionsSnapshot<WorkOption> option, ClientRepository clientRepository)
        {
            r = repository;
            cr = clientRepository;
            _hub = hub;
            //获取 销售计划 企业微信应用的AccessToken
            this.option = option.Value;
        }
        #region POST
        [HttpPost]
        public ResultJSON<SalesPlan> Post([FromBody]SalesPlan s)
        {
            //判断是否重复单号
            if(r.Has(sp => sp.Name == s.Name))
                return new ResultJSON<SalesPlan> { Code = 502 };

            //更新客户默认商品
            if (!cr.SaveDefaultProduct(s.CarNo, s.ProductId))
                return new ResultJSON<SalesPlan> { Code = 501, Msg = "无法更新客户默认商品，请联系开发人员" };

            //标识“陆上”和“水上”的单
            s.IsWater = s.SalesPlanType == SalesPlanType.水上加油 || s.SalesPlanType == SalesPlanType.水上机油 ? true : false;

            r.CurrentUser = UserName;
            SalesPlan result = r.Insert(s);

            if(s.SalesPlanType == SalesPlanType.水上加油 || s.SalesPlanType == SalesPlanType.水上机油) {
                //推送到“水上计划”
                this.option.水上计划AccessToken = AccessTokenContainer.TryGetToken(this.option.CorpId, this.option.水上计划Secret);
                MassApi.SendTextCard(option.水上计划AccessToken, option.水上计划AgentId, $"【水上】{UserName}开出计划单"
                         , $"<div class=\"gray\">单号：{result.Name}</div>" +
                         $"<div class=\"normal\">开单人：{UserName}</div>" +
                         $"<div class=\"normal\">船号/车号：{result.CarNo}</div>" +
                         $"<div class=\"normal\">单价：{result.Price}</div>" +
                         $"<div class=\"normal\">油品：{result.OilName}</div>"
                         , $"https://vue.car0774.com/#/sales/plan/{result.Id}/plan", toUser: "@all");
                //推送到“水上计划审核”
                this.option.水上计划审核AccessToken = AccessTokenContainer.TryGetToken(this.option.CorpId, this.option.水上计划审核Secret);
                MassApi.SendTextCard(option.水上计划审核AccessToken, option.水上计划审核AgentId, $"{UserName}开计划单，请审核"
                         , $"<div class=\"gray\">单号：{result.Name}</div>" +
                         $"<div class=\"normal\">船号/车号：{result.CarNo}</div>" +
                         $"<div class=\"normal\">油品：{result.OilName}</div>"
                         , $"https://vue.car0774.com/#/sales/auditing/false", toUser: "@all");
            }
            else if(s.SalesPlanType == SalesPlanType.陆上装车 || s.SalesPlanType == SalesPlanType.汇鸿车辆加油 || s.SalesPlanType == SalesPlanType.外来车辆加油) {
                this.option.陆上计划AccessToken = AccessTokenContainer.TryGetToken(this.option.CorpId, this.option.陆上计划Secret);
                MassApi.SendTextCard(option.陆上计划AccessToken, option.陆上计划AgentId, $"【陆上】{UserName}开出计划单"
                         , $"<div class=\"gray\">单号：{result.Name}</div>" +
                         $"<div class=\"normal\">开单人：{UserName}</div>" +
                         $"<div class=\"normal\">车牌号：{result.CarNo}</div>" +
                         $"<div class=\"normal\">油品：{result.OilName}</div>"
                         , $"https://vue.car0774.com/#/sales/plan/{result.Id}/plan", toUser: "@all");
                //推送到“陆上计划审核”
                this.option.陆上计划审核AccessToken = AccessTokenContainer.TryGetToken(this.option.CorpId, this.option.陆上计划审核Secret);
                MassApi.SendTextCard(option.陆上计划审核AccessToken, option.陆上计划审核AgentId, $"{UserName}开计划单，请审核"
                         , $"<div class=\"gray\">单号：{result.Name}</div>" +
                         $"<div class=\"normal\">车牌号：{result.CarNo}</div>" +
                         $"<div class=\"normal\">油品：{result.OilName}</div>"
                         , $"https://vue.car0774.com/#/sales/auditing/true", toUser: "@all");
            }
            
            return new ResultJSON<SalesPlan>
            {
                Code = 0,
                Data = result
            };
        }
        #endregion
        #region GET
        [HttpGet("SalesPlanNo")]
        public ResultJSON<string> SalesPlanNo()
        {
            //throw new Exception("测试异常");

            return new ResultJSON<string>
            {
                Code = 0,
                Data = r.GetSerialNumber(r.GetLastSalesPlanNo())
            };
        }
        [HttpGet]
        public ResultJSON<List<SalesPlan>> Get()
        {
            return new ResultJSON<List<SalesPlan>>
            {
                Code = 0,
                Data = r.GetAllList()
            };
        }
        [HttpGet("[action]")]
        public ResultJSON<List<SalesPlan>> Unfinish(string kw, bool isWater, int page, int pagesize)
        {
            DateTime endTime = DateTime.Now;
            DateTime beginTime = DateTime.Now.AddDays(-31);
            List<SalesPlan> list;
            if(string.IsNullOrEmpty(kw))
                list = r.LoadPageList(page, pagesize, out int rowCount, true, false, sp => sp.State != SalesPlanState.已完成 
                && sp.CreatedAt >= beginTime 
                && sp.CreatedAt <= endTime 
                && sp.IsWater == isWater).ToList();
            else
                list = r.LoadPageList(page, pagesize, out int rowCount, true, false, sp => sp.CarNo.Contains(kw) 
                && sp.State != SalesPlanState.已完成 
                && sp.CreatedAt >= beginTime && sp.CreatedAt <= endTime
                && sp.IsWater == isWater).ToList();
            return new ResultJSON<List<SalesPlan>>
            {
                Code = 0,
                Data = list
            };
        }
        /// <summary>
        /// 分页显示数据
        /// </summary>
        /// <param name="page">第N页</param>
        /// <param name="pageSize">页记录数</param>
        /// <param name="type">陆上|水上</param>
        /// <param name="isLeader">是否上级</param>
        /// <returns></returns>
        [HttpGet("[action]")]
        public ResultJSON<List<SalesPlan>> GetByPager(int page, int pageSize, SalesPlanType type, bool isLeader)
        {
            List<SalesPlan> list;
            if(type == SalesPlanType.水上加油)//客户要求“水上部”的人同时可以看到机油类的数据
            { 
                if (isLeader)
                    list = r.LoadPageList(page, pageSize, out int rCount, true, false, s => s.SalesPlanType == type || s.SalesPlanType == SalesPlanType.水上机油).OrderByDescending(s => s.Id).ToList();
                else
                    list = r.LoadPageList(page, pageSize, out int rCount, true, false, s => (s.SalesPlanType == type || s.SalesPlanType == SalesPlanType.水上机油) && s.CreatedBy == UserName).OrderByDescending(s => s.Id).ToList();
            }
            else
            { 
                if(isLeader)
                    list = r.LoadPageList(page, pageSize, out int rCount, true, false, s => s.SalesPlanType == type).OrderByDescending(s => s.Id).ToList();
                else
                    list = r.LoadPageList(page, pageSize, out int rCount, true, false, s => s.SalesPlanType == type && s.CreatedBy == UserName).OrderByDescending(s => s.Id).ToList();
            }
            return new ResultJSON<List<SalesPlan>>
            {
                Code = 0,
                Data = list
            };
        }
        /// <summary>
        /// 根据状态分页显示数据
        /// </summary>
        /// <param name="page">第N页</param>
        /// <param name="pageSize">页记录数</param>
        /// <param name="sps">State状态</param>
        /// <param name="islandplan">是否陆上计划</param>
        /// <returns></returns>
        [HttpGet("[action]")]
        public ResultJSON<List<SalesPlan>> GetByState(int page, int pageSize, SalesPlanState sps, bool islandplan)
        {
            List<SalesPlan> list;
            if (islandplan)
                list = r.LoadPageList(page, pageSize, out int rCount, true, false, s => s.State == sps 
                    && (s.SalesPlanType == SalesPlanType.陆上装车 || s.SalesPlanType == SalesPlanType.汇鸿车辆加油 || s.SalesPlanType == SalesPlanType.外来车辆加油))
                    .OrderByDescending(s => s.Id).ToList();
            else
                list = r.LoadPageList(page, pageSize, out int rCount, true, false, s => s.State == sps 
                    && (s.SalesPlanType == SalesPlanType.水上加油 || s.SalesPlanType == SalesPlanType.水上机油))
                    .OrderByDescending(s => s.Id).ToList();
            return new ResultJSON<List<SalesPlan>>
            {
                Code = 0,
                Data = list
            };
        }
        [HttpGet("[action]/{id}")]
        public ResultJSON<SalesPlan> GetDetail(int id)
        {
            SalesPlan sp = r.GetDetail(id);
            return new ResultJSON<SalesPlan>
            {
                Code = 0,
                Data = sp
            };
        }
        [HttpGet("[action]")]
        public ResultJSON<List<SalesPlan>> GetAuditings(int page, int pagesize, bool islandplan = false)
        {
            List<SalesPlan> list;
            if (islandplan)
                list = r.LoadPageList(page, pagesize, out int rowCount, true, false,
                    s => (s.State == SalesPlanState.已审批 || s.State == SalesPlanState.未审批)
                    && (s.SalesPlanType == SalesPlanType.陆上装车 || s.SalesPlanType == SalesPlanType.汇鸿车辆加油 || s.SalesPlanType == SalesPlanType.外来车辆加油)).ToList();
            else
                list = r.LoadPageList(page, pagesize, out int rowCount, true, false,
                    s => (s.State == SalesPlanState.已审批 || s.State == SalesPlanState.未审批)
                    && (s.SalesPlanType == SalesPlanType.水上加油 || s.SalesPlanType == SalesPlanType.水上机油)).ToList();
            return new ResultJSON<List<SalesPlan>>
            {
                Code = 0,
                Data = list
            };
        }
        [HttpGet("{sv}")]
        public ResultJSON<List<SalesPlan>> Get(string sv)
        {
            return new ResultJSON<List<SalesPlan>>
            {
                Code = 0,
                Data = r.GetAllList(s => s.CarNo.Contains(sv))
            };
        }
        #endregion
        #region PUT
        /// <summary>
        /// 审核计划 设置状态State为已审核
        /// </summary>
        /// <param name="sp">model</param>
        /// <returns></returns>
        [HttpPut("[action]")]
        public ResultJSON<SalesPlan> AuditingOK([FromBody]SalesPlan sp)
        {
            sp.State = SalesPlanState.已审批;
            sp.Auditor = UserName;
            sp.AuditTime = DateTime.Now;
            return new ResultJSON<SalesPlan>
            {
                Code = 0,
                Data = r.Update(sp)
            };
        }
        /// <summary>
        /// 作废单据
        /// </summary>
        /// <param name="id"></param>
        /// <returns>所作废的对象</returns>
        [HttpPut("[action]")]
        public ResultJSON<SalesPlan> Del(int id)
        {
            var sp = r.SetIsDel(id);
            return new ResultJSON<SalesPlan>
            {
                Code = 0,
                Data = sp
            };
        }
        #endregion
    }
}
