﻿<template>
    <div id="root">
        <div v-show="showSelectWorker">
            <div class="font16 align-center" style="font-weight:bold;margin:10px 0">请选择当前施工员：{{worker}}</div>
            <yd-cell-group>
                <yd-cell-item type="radio" v-for="w,idx in workers" :key="idx">
                    <span slot="left">{{w.name}}</span>
                    <input slot="right" type="radio" :value="w.name" v-model="worker" />
                </yd-cell-item>
            </yd-cell-group>
            <div class="align-center">
                <yd-button style="width:90%;height:38px;" @click.native="workerSelectedClick" class="mtop20" :disabled="worker == null || worker == ''">下一步</yd-button>
            </div>
        </div>
        <div v-show="!showSelectWorker">
            <div class="align-center first-group">
                <yd-button style="width:90%;height:38px;" type="primary" @click.native="showOrdersclick" :disabled="oid != null && oid != ''">销售单{{order.name? '：' + order.name : ''}}</yd-button>
            </div>
            <yd-step :current="currStep" style="margin: .4rem 0 .4rem">
                <yd-step-item>
                    <span slot="bottom">选择销售仓</span>
                </yd-step-item>
                <yd-step-item>
                    <span slot="bottom">加油</span>
                </yd-step-item>
                <yd-step-item>
                    <span slot="bottom">完工</span>
                </yd-step-item>
            </yd-step>
            <div class="align-center" v-show="currStep == 1">
                <yd-button style="width:90%;height: 38px" type="primary" @click.native="showStores = true">选择销售仓</yd-button>
            </div>
            <!--明细-->
            <yd-cell-group title="施工明细" v-show="currStep == 2 || currStep == 3">
                <yd-cell-item>
                    <span slot="left">数量：</span>
                    <span slot="right">{{order.count}}</span>
                </yd-cell-item>
                <yd-cell-item>
                    <span slot="left">{{carOrBoat}}：</span>
                    <span slot="right">{{order.carNo}}</span>
                </yd-cell-item>
                <yd-cell-item>
                    <span slot="left">销售仓：</span>
                    <span slot="right">{{order.store != null ? order.store.name : ''}}</span>
                </yd-cell-item>
                <yd-cell-item>
                    <span slot="left">施工人员：</span>
                    <span slot="right">{{order.worker}}</span>
                </yd-cell-item>
            </yd-cell-group>
            <div class="align-center" v-show="currStep == 2">
                <yd-button style="width:90%;height:38px;margin-top: 30px;" type="primary" @click.native="currStep -= 1">← 上一步：选择销售仓</yd-button>
                <yd-button style="width:90%;height:38px;margin-top: 10px;" type="primary" @click.native="changeState(5)">下一步：完工 →</yd-button>
            </div>
        </div>
        <!--popup订单选择-->
        <yd-popup v-model="showOrders" position="right" width="70%">
            <yd-pullrefresh :callback="getOrders">
                <yd-cell-group>
                    <yd-cell-item v-for="o in orders" :key="o.id" @click.native="orderclick(o)" arrow>
                        <div slot="left" style="padding:.2rem 0 .2rem">
                            <p>{{o.carNo}}</p>
                            <p style="color:lightgray">{{o.name}}</p>
                        </div>
                        <div slot="right" style="text-align: left;margin-right: 5px">
                            <p class="col-gray">{{o.product.name}}</p>
                            <p class="col-gray">{{o.count}}升</p>
                        </div>
                    </yd-cell-item>
                </yd-cell-group>
            </yd-pullrefresh>
        </yd-popup>
        <!--popup销售仓选择-->
        <yd-popup v-model="showStores" position="right">
            <yd-cell-group title="请选择销售仓">
                <yd-cell-item v-for="s in stores" :key="s.id" @click.native="storeclick(s)">
                    <div slot="left">
                        <p>{{s.name}}</p>
                    </div>
                    <div slot="right">
                        <p class="col-light-gray">{{s.value}}</p>
                    </div>
                </yd-cell-item>
            </yd-cell-group>
        </yd-popup>
    </div>
</template>

<script src="./load.ts" />

