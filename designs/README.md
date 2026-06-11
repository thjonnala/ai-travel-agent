# Design diagrams

Design documentation for the Smart AI Travel Agent. Each diagram exists as an
editable source plus scalable SVG and high-resolution PNG renders.

| # | Diagram | Source | SVG | PNG |
|---|---|---|---|---|
| 4 | Class — entities, services, interfaces | [mmd](04-class-diagram.mmd) | [svg](04-class-diagram.svg) | [png](04-class-diagram.png) |
| 5 | Entity-Relationship — tables, keys, cardinality | [mmd](05-er-diagram.mmd) | [svg](05-er-diagram.svg) | [png](05-er-diagram.png) |
| 8 | Azure architecture flow — tiers + AI planning, Azure icon style | [svg = source](08-travel-agent-azure-flow.svg) | [svg](08-travel-agent-azure-flow.svg) | [png](08-travel-agent-azure-flow.png) |
| 9 | Azure sequence flow — lifelines + numbered messages, Azure icon style | [svg = source](09-travel-agent-azure-sequence.svg) | [svg](09-travel-agent-azure-sequence.svg) | [png](09-travel-agent-azure-sequence.png) |
| 11 | Azure deployment flow — CI/CD pipelines with stages | [svg = source](11-travel-agent-azure-deployment-flow.svg) | [svg](11-travel-agent-azure-deployment-flow.svg) | [png](11-travel-agent-azure-deployment-flow.png) |
| 12 | Azure network diagram — runtime traffic, ports, security posture | [svg = source](12-travel-agent-azure-network.svg) | [svg](12-travel-agent-azure-network.svg) | [png](12-travel-agent-azure-network.png) |

See **[designs.md](designs.md)** for a visual gallery of all diagrams.

## Editing & regenerating

**Mermaid diagrams (4, 5):** edit the `.mmd` files (they also render inline on
GitHub/Azure DevOps and at [mermaid.live](https://mermaid.live)), then:

```powershell
cd designs
# puppeteer-config.json points at a locally installed Chrome
npx -y -p @mermaid-js/mermaid-cli mmdc -p puppeteer-config.json -i <name>.mmd -o <name>.svg -b white
npx -y -p @mermaid-js/mermaid-cli mmdc -p puppeteer-config.json -i <name>.mmd -o <name>.png -b white -s 3
```

**Azure icon-style diagrams (8, 9, 11, 12):** the SVG is the source — edit it
directly, then re-render the PNG by screenshotting it in a headless browser:

```powershell
# wrapper.html: <img src="<name>.svg" style="width:3600px;height:2000px"> with margin:0/overflow:hidden
chrome --headless --hide-scrollbars --screenshot=<name>.png --window-size=3600,2000 wrapper.html
```
