# Harness Delivery Template

每次 backend 改动默认按以下结构交付：

## 1. Requirement Frame

- 目标是什么
- 影响哪些服务、接口、配置、数据
- 正常路径、失败路径、幂等或回滚约束是什么

## 2. Change Plan

- 根因定位
- 最小闭环改动点
- 兼容性与回滚面

## 3. Validation

- 已执行的 build / test / 类型检查 / 最小链路验证
- 未验证范围与原因

## 4. Observability

- 新增或依赖的日志、状态、诊断点
- 生产问题如何快速定位

## 5. Delivery Summary

- 已实现
- 已验证
- 剩余风险
- 下一步建议
