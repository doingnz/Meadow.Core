﻿namespace Meadow.Hardware
{
    public interface IF7CoreComputePinout : I32PinFeatherBoardPinout, IPinDefinitions
    {
        IPin I2C1_SCL { get; }
        IPin I2C1_SDA { get; }
        IPin I2C3_SCL { get; }
        IPin I2C3_SDA { get; }
        IPin SPI3_SCK { get; }
        IPin SPI3_COPI { get; }
        IPin SPI3_CIPO { get; }
        IPin SPI5_SCK { get; }
        IPin SPI5_COPI { get; }
        IPin SPI5_CIPO { get; }
        IPin D16 { get; }
        IPin D17 { get; }
        IPin D18 { get; }
        IPin D19 { get; }
        IPin D20 { get; }

        IPin PA0 { get; }
        IPin PA1_ETH_REF_CLK { get; }
        IPin PA2_ETH_MDIO { get; }
        IPin PA3 { get; }
        IPin PA4 { get; }
        IPin PA5 { get; }
        IPin PA7_ETH_CRS_DV { get; }
        IPin PA9 { get; }
        IPin PA10 { get; }
        IPin PA13 { get; }
        IPin PA14 { get; }
        IPin PA15 { get; }
        IPin PB0 { get; }
        IPin PB1 { get; }
        IPin PB3 { get; }
        IPin PB4 { get; }
        IPin PB5 { get; }
        IPin PB6 { get; }
        IPin PB7 { get; }
        IPin PB8 { get; }
        IPin PB9 { get; }
        IPin PB11_ETH_TX_EN { get; }
        IPin PB12 { get; }
        IPin PB13 { get; }
        IPin PB14 { get; }
        IPin PB15 { get; }
        IPin PC0 { get; }
        IPin PC1_ETH_MDC { get; }
        IPin PC2 { get; }
        IPin PC4_ETH_RXD0 { get; }
        IPin PC5_ETH_RXD1 { get; }
        IPin PC6 { get; }
        IPin PC7 { get; }
        IPin PC8 { get; }
        IPin PC9 { get; }
        IPin PC10 { get; }
        IPin PC11 { get; }
        IPin PD5 { get; }
        IPin PD6_SDMMC_CLK { get; }
        IPin PD7_SDMMC_CMD { get; }
        IPin PF8 { get; }
        IPin PF9 { get; }
        IPin PG6_SDMMC_IN_L { get; }
        IPin PG9_SDMMC_D0 { get; }
        IPin PG10_SDMMC_D1 { get; }
        IPin PG11_SDMMC_D2 { get; }
        IPin PG12_SDMMC_D3 { get; }
        IPin PG13_ETH_TXD0 { get; }
        IPin PG14_ETH_TXD1 { get; }
        IPin PH6 { get; }
        IPin PH7 { get; }
        IPin PH8 { get; }
        IPin PH10 { get; }
        IPin PH12 { get; }
        IPin PH13 { get; }
        IPin PH14_ETH_IRQ { get; }
        IPin PI9 { get; }
        IPin PI11 { get; }
    }
}